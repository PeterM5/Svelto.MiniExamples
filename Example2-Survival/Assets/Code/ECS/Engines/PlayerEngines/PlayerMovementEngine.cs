using System.Collections;
using Svelto.Tasks;
using UnityEngine;

namespace Svelto.ECS.Example.Survive.Characters.Player
{
    public class PlayerMovementEngine
        : SingleEntityReactiveEngine<PlayerEntityViewStruct>, IQueryingEntitiesEngine, IStep<PlayerDeathCondition>
    {
        const float camRayLength = 100f; // The length of the ray from the camera into the scene.

        readonly IRayCaster                _rayCaster;
        readonly ITaskRoutine<IEnumerator> _taskRoutine;
        readonly ITime                     _time;

        readonly int
            floorMask = LayerMask
               .GetMask("Floor"); // A layer mask so that a ray can be cast just at gameobjects on the floor layer.

        public PlayerMovementEngine(IRayCaster raycaster, ITime time)
        {
            _rayCaster   = raycaster;
            _time        = time;
            _taskRoutine = TaskRunner.Instance.AllocateNewTaskRoutine(StandardSchedulers.physicScheduler);
            _taskRoutine.SetEnumerator(PhysicsTick());
        }

        public IEntitiesDB entitiesDB { private get; set; }

        public void Ready() { }

        public void Step(PlayerDeathCondition condition, EGID id)
        {
            var playerEntityView = entitiesDB.QueryEntities<PlayerEntityViewStruct>(ECSGroups.Player, out var count)[0];
            playerEntityView.rigidBodyComponent.isKinematic = true;
        }

        protected override void Add(ref PlayerEntityViewStruct           playerEntityViewStruct,
                                    ExclusiveGroup.ExclusiveGroupStruct? previousGroup)
        {
            _taskRoutine.Start();
        }

        protected override void Remove(ref PlayerEntityViewStruct playerEntityViewStruct, bool itsaSwap)
        {
            _taskRoutine.Stop();
        }

        IEnumerator PhysicsTick()
        {
            while (true)
            {
                var playerEntities =
                    entitiesDB.QueryEntities<PlayerEntityViewStruct, PlayerInputDataStruct>(ECSGroups.Player,
                                                                                            out var targetsCount);

                for (var i = 0; i < targetsCount; i++)
                {
                    Movement(ref playerEntities.Item2[i], ref playerEntities.Item1[i]);
                    Turning(ref playerEntities.Item2[i], ref playerEntities.Item1[i]);
                }

                yield return null; //don't forget to yield or you will enter in an infinite loop!
            }
        }

        /// <summary>
        ///     In order to keep the class testable, we need to reduce the number of
        ///     dependencies injected through static classes at its minimum.
        ///     Implementors are the place where platform dependencies can be transformed into
        ///     entity components, so that here we can use inputComponent instead of
        ///     the class Input.
        /// </summary>
        /// <param name="playerEntityView"></param>
        /// <param name="entityView"></param>
        void Movement(ref PlayerInputDataStruct playerEntityView, ref PlayerEntityViewStruct entityView)
        {
            // Store the input axes.
            var input = playerEntityView.input;

            // Normalise the movement vector and make it proportional to the speed per second.
            var movement = input.normalized * entityView.speedComponent.movementSpeed * _time.deltaTime;

            // Move the player to it's current position plus the movement.
            entityView.transformComponent.position = entityView.positionComponent.position + movement;
        }

        void Turning(ref PlayerInputDataStruct playerEntityView, ref PlayerEntityViewStruct entityView)
        {
            // Create a ray from the mouse cursor on screen in the direction of the camera.
            var camRay = playerEntityView.camRay;

            // Perform the raycast and if it hits something on the floor layer...
            Vector3 point;
            if (_rayCaster.CheckHit(camRay, camRayLength, floorMask, out point))
            {
                // Create a vector from the player to the point on the floor the raycast from the mouse hit.
                var playerToMouse = point - entityView.positionComponent.position;

                // Ensure the vector is entirely along the floor plane.
                playerToMouse.y = 0f;

                // Create a quaternion (rotation) based on looking down the vector from the player to the mouse.
                var newRotatation = Quaternion.LookRotation(playerToMouse);

                // Set the player's rotation to this new rotation.
                entityView.transformComponent.rotation = newRotatation;
            }
        }
    }
}