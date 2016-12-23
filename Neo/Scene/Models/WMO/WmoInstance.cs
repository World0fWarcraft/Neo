using System;
using Neo.IO.Files.Models;
using Neo.Utils;
using OpenTK;
using SlimTK;

namespace Neo.Scene.Models.WMO
{
	public class WmoInstance : IModelInstance
    {
        private Matrix4 mInstanceMatrix;
        private Matrix4 mInverseInstanceMatrix;

        private WeakReference<WmoRootRender> mRenderer;

        public BoundingBox BoundingBox;

        private WorldText mWorldModelName;

        public BoundingBox InstanceBoundingBox { get { return BoundingBox; } }

        public int Uuid { get; private set; }
        public BoundingBox[] GroupBoxes { get; private set; }
        public Matrix4 InstanceMatrix { get { return mInstanceMatrix; } }
        public Vector3[] InstanceCorners { get; private set; }

        public bool IsSpecial { get { return false; } }

        public WmoRoot ModelRoot { get; private set; }

        public int ReferenceCount;

        private Vector3 mPosition;
        private Vector3 mRotation;

        private WmoRootRender mModel;


        public WmoInstance(int uuid, Vector3 position, Vector3 rotation, WmoRootRender model)
        {
            ReferenceCount = 1;
            Uuid = uuid;
            BoundingBox = model.BoundingBox;

            mPosition = position;
            mRotation = rotation;
            mModel = model;

	        mInstanceMatrix = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(mRotation.X)) *
	                          Matrix4.CreateRotationY(MathHelper.DegreesToRadians(mRotation.Y)) *
	                          Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(mRotation.Z));

	        mRenderer = new WeakReference<WmoRootRender>(model);

            InstanceCorners = model.BoundingBox.GetCorners();
	        // TODO: Find correct function to use here
            Vector3.TransformVector(InstanceCorners, ref mInstanceMatrix, InstanceCorners);

            BoundingBox = BoundingBox.Transform(ref mInstanceMatrix);
            GroupBoxes = new BoundingBox[model.Groups.Count];
            for(var i = 0; i < GroupBoxes.Length; ++i)
            {
                var group = model.Groups[i];
                GroupBoxes[i] = group.BoundingBox.Transform(ref mInstanceMatrix);
            }
            Matrix4.Invert(ref mInstanceMatrix, out mInverseInstanceMatrix);

            mInstanceMatrix = Matrix4.Transpose(mInstanceMatrix);
            ModelRoot = model.Data;
        }

        public bool Intersects(IntersectionParams parameters, ref Ray globalRay, out float distance)
        {
            distance = float.MaxValue;
            if (globalRay.Intersects(ref BoundingBox) == false)
            {
	            return false;
            }

	        WmoRootRender renderer;
            if (mRenderer.TryGetTarget(out renderer) == false)
            {
	            return false;
            }

	        var instRay = Picking.Build(ref parameters.ScreenPosition, ref parameters.InverseView,
                ref parameters.InverseProjection, ref mInverseInstanceMatrix);

            var hasHit = false;
            for (var i = 0; i < GroupBoxes.Length; ++i)
            {
                if (globalRay.Intersects(ref GroupBoxes[i]) == false)
                {
	                continue;
                }

	            float dist;
                if (renderer.Groups[i].Intersects(parameters, ref instRay, out dist) && dist < distance)
                {
                    distance = dist;
                    hasHit = true;
                }
            }

            return hasHit;
        }

        ~WmoInstance()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            DestroyModelNameplate();

            ModelRoot = null;
            mRenderer = null;
        }

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void CreateModelNameplate()
        {
            if (mWorldModelName != null)
            {
	            return;
            }

	        mWorldModelName = new WorldText
            {
                Text = System.IO.Path.GetFileName(ModelRoot.FileName),
                Scaling = 1.0f,
                DrawMode = WorldText.TextDrawMode.TextDraw3D
            };

            UpdateModelNameplate();
            WorldFrame.Instance.WorldTextManager.AddText(mWorldModelName);
        }

        public void Rotate(float x, float y, float z)
        {
            mRotation.X += x;
            mRotation.Y += y;
            mRotation.Z += z;

	        mInstanceMatrix = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(mRotation.X)) *
	                          Matrix4.CreateRotationY(MathHelper.DegreesToRadians(mRotation.Y)) *
	                          Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(mRotation.Z));

	        mInstanceMatrix *= Matrix4.CreateTranslation(mPosition);

            //mRenderer = new WeakReference<WmoRootRender>(mModel);


            Matrix4.Invert(ref mInstanceMatrix, out mInverseInstanceMatrix);

            BoundingBox = mModel.BoundingBox.Transform(ref mInstanceMatrix);
            GroupBoxes = new BoundingBox[mModel.Groups.Count];
            for (var i = 0; i < GroupBoxes.Length; ++i)
            {
                var group = mModel.Groups[i];
                GroupBoxes[i] = group.BoundingBox.Transform(ref mInstanceMatrix);
            }

            InstanceCorners = mModel.BoundingBox.GetCorners();
	        // TODO: Find correct function to use here
	        Vector3.TransformVector(InstanceCorners, ref mInstanceMatrix, InstanceCorners);
            mInstanceMatrix = Matrix4.Transpose(mInstanceMatrix);
            ModelRoot = mModel.Data;
            UpdateModelNameplate();
        }

        public void UpdateScale(float s)
        {
            // Unsupported by WMO.
        }

        public void SetPosition(Vector3 position)
        {
            mPosition += position;

	        mInstanceMatrix = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(mRotation.X)) *
	                          Matrix4.CreateRotationY(MathHelper.DegreesToRadians(mRotation.Y)) *
	                          Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(mRotation.Z));
            //mRenderer = new WeakReference<WmoRootRender>(mModel);

            mInstanceMatrix *= Matrix4.CreateTranslation(mPosition);

            Matrix4.Invert(ref mInstanceMatrix, out mInverseInstanceMatrix);

            BoundingBox = mModel.BoundingBox.Transform(ref mInstanceMatrix); //here is the problem, after this line the bBox is fucked up

            GroupBoxes = new BoundingBox[mModel.Groups.Count];
            for (var i = 0; i < GroupBoxes.Length; ++i)
            {
                var group = mModel.Groups[i];
                GroupBoxes[i] = group.BoundingBox.Transform(ref mInstanceMatrix);
            }

            InstanceCorners = mModel.BoundingBox.GetCorners();
	        // TODO: Find correct function to use here
	        Vector3.TransformVector(InstanceCorners, ref mInstanceMatrix, InstanceCorners);
            mInstanceMatrix = Matrix4.Transpose(mInstanceMatrix);
            ModelRoot = mModel.Data;
            UpdateModelNameplate();
        }

	    [Obsolete]
        public Vector3 GetPosition()
        {
            return mPosition;
        }

	    [Obsolete]
        public Vector3 GetRotation()
        {
            return mRotation;
        }

        public void DestroyModelNameplate()
        {
            if (mWorldModelName == null)
            {
	            return;
            }

	        WorldFrame.Instance.WorldTextManager.RemoveText(mWorldModelName);
            mWorldModelName.Dispose();
            mWorldModelName = null;
        }

        private void UpdateModelNameplate()
        {
            if (mWorldModelName == null)
            {
	            return;
            }

	        Vector3 diff = BoundingBox.Maximum - BoundingBox.Minimum;
            mWorldModelName.Scaling = diff.Length / 60.0f;
            if (mWorldModelName.Scaling < 0.3f)
            {
	            this.mWorldModelName.Scaling = 0.3f;
            }

	        var position = BoundingBox.Minimum + (diff * 0.5f);
            position.Z = 1.5f + BoundingBox.Minimum.Z + (diff.Z * 1.08f);
            mWorldModelName.Position = position;
        }

        public Vector3 GetNamePlatePosition()
        {
            if (mWorldModelName == null)
            {
	            return new Vector3(0.0f,0.0f,0.0f);
            }

	        return mWorldModelName.Position;
        }

        public void Remove()
        {
            WorldFrame.Instance.WmoManager.RemoveInstance(ModelRoot.FileName, Uuid,true);
            WorldFrame.Instance.ClearSelection();
        }

        public string GetModelName()
        {
            return ModelRoot.FileName;
        }
    }
}
