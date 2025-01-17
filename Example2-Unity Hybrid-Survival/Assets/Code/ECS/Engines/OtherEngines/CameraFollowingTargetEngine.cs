using System.Collections;
using UnityEngine;

namespace Svelto.ECS.Example.Survive.Camera
{
    //First step identify the entity type we want the engine to handle: CameraEntity
    //Second step name the engine according the behaviour and the entity: I.E.: CameraFollowTargetEngine
    //Third step start to write the code and create classes/fields as needed using refactoring tools 
    public class CameraFollowingTargetEngine : IQueryingEntitiesEngine, IStepEngine
    {
        public CameraFollowingTargetEngine(ITime time)
        {
            _time   = time;
            _update = Update();
        }

        public void       Ready()    { }
        public EntitiesDB entitiesDB { get; set; }
        public string     name       => nameof(CameraFollowingTargetEngine);

        public void Step() { _update.MoveNext(); }

        IEnumerator Update()
        {
            var smoothing = 5.0f;

            void TrackCameraTarget()
            {
                foreach (var ((targets, cameras, count), _) in entitiesDB
                   .QueryEntities<CameraTargetEntityReferenceComponent, CameraEntityViewComponent>(Camera.Groups))
                {
                    for (uint i = 0; i < count; i++)
                    {
                        ref var camera = ref cameras[i];

                        EntityReference entityReference = targets[i].targetEntity;
                        //The camera target can be destroyed while the camera is still active
                        if (entityReference.ToEGID(entitiesDB, out var targetEGID))
                        {
                            ref var cameraTarget =
                                ref entitiesDB.QueryEntity<CameraTargetEntityViewComponent>(targetEGID);

                            var targetCameraPos = cameraTarget.targetComponent.position + camera.cameraComponent.offset;

                            camera.transformComponent.position = Vector3.Lerp(
                                camera.positionComponent.position, targetCameraPos, smoothing * _time.deltaTime);
                        }
                    }
                }
            }

            while (true)
            {
                TrackCameraTarget();

                yield return null;
            }
        }

        readonly ITime       _time;
        readonly IEnumerator _update;
    }
}