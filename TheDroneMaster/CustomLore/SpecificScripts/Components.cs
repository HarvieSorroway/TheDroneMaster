using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.PlayerLoop;
using static TheDroneMaster.Cool3DObject;
using static UnityEngine.RectTransform;

namespace TheDroneMaster.CustomLore.SpecificScripts
{

    public class RWComponent
    {
        protected RWComponent(SceneComponent root,bool isRoot = false)
        {
            if(isRoot)
                IsRoot = true;
            else
                Assert.IsNotNull(root);

            root.AddChild(this);
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

    public class VeterxComponent : RWComponent
    {
        protected VeterxComponent(SceneComponent root) : base(root)
        {

        }

    }


    public class RotatorComponent : RWComponent
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


    public class SceneComponent : RWComponent
    {
        public SceneComponent(SceneComponent root) : base(root)
        {
        }

        protected SceneComponent(SceneComponent root, bool isRoot = false) : base(root, isRoot)
        {
        }

        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Scale { get; set; }



        public Vector3 WorldPosition
        {
            get
            {
                if (IsRoot) return Position;
                return Root.WorldPosition + WorldOffest;
            }
        } 

        public Vector3 WorldRotation
        {
            get
            {
                if (IsRoot) return Rotation;
                return Root.WorldRotation + Rotation;
            }
        }

        public Vector3 WorldScale
        {
            get
            {
                if (IsRoot) return Scale;

                return Vector3.Scale( Root.WorldScale, Scale);
            }
        }

        public Vector3 WorldOffest
        {
            get
            {
                var axis = GetAxis().ToArray();
                return Position.x * axis[0] + Position.y * axis[1] + Position.z * axis[2];
            }
        }


        public IEnumerable<Vector3> GetAxis()
        {
            var quaternion = Quaternion.AngleAxis(WorldRotation.z, Vector3.forward) *
                             Quaternion.AngleAxis(WorldRotation.x, Vector3.left) * 
                             Quaternion.AngleAxis(WorldRotation.y, Vector3.up);
            yield return quaternion * Vector3.left;
            yield return quaternion * Vector3.up;
            yield return quaternion * Vector3.forward;
        }




        public void AddChild(RWComponent child)
        {
            Children.Add(child);
        }



        public List<RWComponent> Children { get; private set; } = new List<RWComponent>();

    }

}
