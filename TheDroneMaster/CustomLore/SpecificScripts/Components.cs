using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.PlayerLoop;
using static TheDroneMaster.CustomLore.SpecificScripts.Small3DObject;

namespace TheDroneMaster.CustomLore.SpecificScripts
{

    public class Component
    {
        protected Component(SceneComponent root,bool isRoot = false)
        {
            if(isRoot)
                IsRoot = true;
            else
                Assert.IsNotNull(root);
        }
        public virtual void Update()
        {

        }
        public bool IsRoot { get; private set; } = false;

        public SceneComponent Root { get; private set; }
    }


    public class ActorComponent : SceneComponent
    {
        public ActorComponent(Mesh3D small3DObject) : base(null,true)
        {
            ownObject = small3DObject;
        }

        public override void Update()
        {
            base.Update();
            ownObject.Update();
        }
        public Mesh3D ownObject;
    }

    public class VeterxComponent : Component
    {
        protected VeterxComponent(SceneComponent root) : base(root)
        {

        }

    }


    public class RotatorComponent : Component
    {
        public RotatorComponent(SceneComponent root, Vector3 rotationVel) : base(root)
        {
            RotationVel = rotationVel;
        }
        public override void Update()
        {
            base.Update();
            Root.Rotation += RotationVel;
        }
        public Vector3 RotationVel;
    }


    public class SceneComponent : Component
    {
        public SceneComponent(SceneComponent root) : base(root)
        {
        }

        protected SceneComponent(SceneComponent root, bool isRoot = false) : base(root, isRoot)
        {
        }

        public override void Update()
        {
            base.Update();
        }

        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scale;


        public Vector3 GetRelativePosition()
        {
            if (IsRoot) return Position;
            return (TransformType.Relative - Type) * Root.GetWorldPosition() + Position;
        }

        public Vector3 GetRelativeRotation()
        {
            if (IsRoot) return Rotation;
            return (TransformType.Relative - Type) * Root.GetWorldRotation() + Rotation;
        }

        public Vector3 GetWorldPosition()
        {
            if (IsRoot) return Position;

            return (TransformType.World - Type) * Root.GetWorldPosition() + Position;
        } 

        public Vector3 GetWorldRotation()
        {
            if (IsRoot) return Rotation;

            return (TransformType.World - Type) * Root.GetWorldRotation() + Rotation;
        }

        public Vector3 GetWorldScale()
        {
            if (IsRoot) return Scale;

            return Vector3.Scale(((TransformType.World - Type) * Root.GetWorldScale()) , Scale);
        }

        public void SetTransformType(TransformType type)
        {
            var offest = type - Type;
            Type = type;
            if (IsRoot || offest == 0)
                return;
            Position += offest * Root.GetWorldPosition();
            Rotation += offest * Root.GetWorldRotation();

            //TODO
            //Scale *= (offest == 1) ? Root.GetWorldScale() : new Vector3(1,1,1) / Root.GetWorldScale();

        }

        public TransformType Type { get; private set; } = TransformType.Relative;

        public enum TransformType
        {
            World = 1,
            Relative = 0,
        }
    }

}
