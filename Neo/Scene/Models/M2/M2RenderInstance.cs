﻿using System;
using Neo.Utils;
using Neo.IO.Files.Models;
using OpenTK;
using OpenTK.Graphics;
using SlimTK;

namespace Neo.Scene.Models.M2
{
    public class M2RenderInstance : IModelInstance
    {
        private Matrix4 mInstanceMatrix;
        private Matrix4 mInverseMatrix;
        private Matrix4 mInverseRotation;

        private Vector3 mPosition;
        private Vector3 mRotation;
        private Color4 mHighlightColor = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
        private Vector3 mScale;

        private bool mIsHighlighted;
        private bool mHighlightFinished;
        private TimeSpan mHighlightStartTime;

        private M2File mModel;
        private M2Renderer mRenderer;
        private BoundingBox mBoundingBox;

        private WorldText mWorldModelName;

        public Vector3 Position { get { return mPosition; } }
        public float Scale { get { return mScale.X; } }
        public Vector3 Rotation { get { return mRotation; } }

        public M2File Model { get { return mModel; } }

        public M2Renderer Renderer { get { return mRenderer; } }

        public BoundingBox BoundingBox { get { return mBoundingBox; } }
        public BoundingBox InstanceBoundingBox { get { return BoundingBox; } }

        public Vector3[] InstanceCorners { get; private set; }

        public bool IsUpdated { get; set; }

        public bool IsSpecial { get { return Uuid == Editing.ModelSpawnManager.M2InstanceUuid; } }

        public int Uuid { get; private set; }

        public Matrix4 InstanceMatrix { get { return mInstanceMatrix; } }
        public Matrix4 InverseMatrix { get { return mInverseMatrix; } }
        public Matrix4 InverseRotation { get { return mInverseRotation; } }

        public Color4 HighlightColor { get { return mHighlightColor; } }

        public float Depth { get; private set; }

        public int NumReferences { get; set; }

        public M2RenderInstance(int uuid, Vector3 position, Vector3 rotation, Vector3 scale, M2Renderer renderer)
        {
            mScale = scale;
            mPosition = position;
            mRotation = rotation;
            NumReferences = 1;
            Uuid = uuid;

            mRenderer = renderer;
            mModel = mRenderer.Model;
            mBoundingBox = mModel.BoundingBox;

            var rotationMatrix = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(mRotation.X)) *
							     Matrix4.CreateRotationY(MathHelper.DegreesToRadians(mRotation.Y)) *
							     Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(mRotation.Z));

	        Matrix4.Invert(ref rotationMatrix, out mInverseRotation);
            mInstanceMatrix = rotationMatrix * Matrix4.CreateScale(scale) * Matrix4.CreateTranslation(position);
            mBoundingBox = BoundingBox.Transform(ref mInstanceMatrix);
            Matrix4.Invert(ref mInstanceMatrix, out mInverseMatrix);

            InstanceCorners = mModel.BoundingBox.GetCorners();

	        // TODO: Investigate correct function to use here
            Vector3.TransformVector(InstanceCorners, ref mInstanceMatrix, InstanceCorners);
        }

        ~M2RenderInstance()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            DestroyModelNameplate();

            mModel = null;
            mRenderer = null;
        }

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool Intersects(IntersectionParams parameters, ref Ray globalRay, out float value)
        {
            value = float.MaxValue;

            if (globalRay.Intersects(ref mBoundingBox) == false)
            {
	            return false;
            }

	        var instRay = Picking.Build(ref parameters.ScreenPosition, ref parameters.InverseView,
                ref parameters.InverseProjection, ref mInverseMatrix);
            return mModel.Intersect(ref instRay, out value);
        }

        public void Rotate(float x, float y, float z)
        {
            mRotation.X += x;
            mRotation.Y += y;
            mRotation.Z += z;

	        var rotationMatrix = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(mRotation.X)) *
	                             Matrix4.CreateRotationY(MathHelper.DegreesToRadians(mRotation.Y)) *
	                             Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(mRotation.Z));

	        Matrix4.Invert(ref rotationMatrix, out mInverseRotation);

            mInstanceMatrix = rotationMatrix * Matrix4.CreateScale(mScale) * Matrix4.CreateTranslation(mPosition);
            Matrix4.Invert(ref mInstanceMatrix, out mInverseMatrix);

            mBoundingBox = mModel.BoundingBox.Transform(ref mInstanceMatrix);

            InstanceCorners = mModel.BoundingBox.GetCorners();

	        // TODO: Investigate correct function to use here
	        Vector3.TransformVector(InstanceCorners, ref mInstanceMatrix, InstanceCorners);
            UpdateModelNameplate();
        }

        public void SetPosition(Vector3 position)
        {
            mPosition.X += position.X;
            mPosition.Y += position.Y;
            mPosition.Z += position.Z;

	        var rotationMatrix = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(mRotation.X)) *
	                             Matrix4.CreateRotationY(MathHelper.DegreesToRadians(mRotation.Y)) *
	                             Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(mRotation.Z));

	        Matrix4.Invert(ref rotationMatrix, out mInverseRotation);

            mInstanceMatrix = rotationMatrix * Matrix4.CreateScale(mScale) * Matrix4.CreateTranslation(mPosition);
            Matrix4.Invert(ref mInstanceMatrix, out mInverseMatrix);

            mBoundingBox = mModel.BoundingBox.Transform(ref mInstanceMatrix);

            InstanceCorners = mModel.BoundingBox.GetCorners();

	        // TODO: Investigate correct function to use here
	        Vector3.TransformVector(InstanceCorners, ref mInstanceMatrix, InstanceCorners);
            UpdateModelNameplate();
        }

	    [Obsolete]
        public Vector3 GetRotation()
        {
            return mRotation;
        }

	    [Obsolete]
        public Vector3 GetPosition()
        {
            return mPosition;
        }

        public void UpdateScale(float scale)
        {
            mScale.X += scale;
            mScale.Y += scale;
            mScale.Z += scale;

            if (mScale.X < 0.0f)
            {
                mScale.X = 0.0f;
                mScale.Y = 0.0f;
                mScale.Z = 0.0f;
            }

            if (mScale.X > 63.9f)
            {
                mScale.X = 63.9f;
                mScale.Y = 63.9f;
                mScale.Z = 63.9f;
            }

	        var rotationMatrix = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(mRotation.X)) *
	                             Matrix4.CreateRotationY(MathHelper.DegreesToRadians(mRotation.Y)) *
	                             Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(mRotation.Z));

	        Matrix4.Invert(ref rotationMatrix, out mInverseRotation);

            mInstanceMatrix = rotationMatrix * Matrix4.CreateScale(mScale) * Matrix4.CreateTranslation(mPosition);
            Matrix4.Invert(ref mInstanceMatrix, out mInverseMatrix);

            mBoundingBox = mModel.BoundingBox.Transform(ref mInstanceMatrix);

            InstanceCorners = mModel.BoundingBox.GetCorners();

	        // TODO: Investigate correct function to use here
	        Vector3.TransformVector(InstanceCorners, ref mInstanceMatrix, InstanceCorners);
            UpdateModelNameplate();
        }

        public void CreateModelNameplate()
        {
            if (mWorldModelName != null)
            {
	            return;
            }

	        mWorldModelName = new WorldText
            {
                Text = mModel.ModelName,
                Scaling = 1.0f,
                DrawMode = WorldText.TextDrawMode.TextDraw3D
            };

            UpdateModelNameplate();
            WorldFrame.Instance.WorldTextManager.AddText(mWorldModelName);
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

	        Vector3 diff = mBoundingBox.Minimum - mBoundingBox.Maximum;
            mWorldModelName.Scaling = diff.Length / 60.0f;
            if (mWorldModelName.Scaling < 0.3f)
            {
	            this.mWorldModelName.Scaling = 0.3f;
            }

	        Vector3 position = mBoundingBox.Minimum + (diff * 0.5f);
            position.Z = 1.5f + mBoundingBox.Minimum.Z + (diff.Z * 1.08f);
            mWorldModelName.Position = position;
        }

        private void UpdateHighlightColor(Color4 highlightColor)
        {
            mHighlightColor = highlightColor;
        }

        public bool IsVisible(Camera camera)
        {
            return camera.Contains(ref mBoundingBox);
        }

        public void UpdateBrushHighlighting(Vector3 brushPosition, float radius)
        {
            var targetVec = mPosition - brushPosition;
            var distance = targetVec.LengthSquared;
            var radiusSquared = radius * radius;

            var time = TimeManager.Instance.GetTime();
            var timeDelta = time - mHighlightStartTime;
            var timeMs = timeDelta.TotalMilliseconds;

            var src = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
            var dst = new Color4(1.5f, 1.5f, 1.5f, 1.0f);

            var fac = (float)(timeMs / 500.0);
            if (fac > 1.0f)
            {
	            fac = 1.0f;
            }

	        if (distance < radiusSquared)
            {
                if (!mIsHighlighted)
                {
                    mHighlightStartTime = time;
                    mIsHighlighted = true;
                    mHighlightFinished = false;
                    return;
                }
            }
            else
            {
                if (mIsHighlighted)
                {
                    mHighlightStartTime = time;
                    mIsHighlighted = false;
                    mHighlightFinished = false;
                    return;
                }

                fac = 1.0f - fac;
            }

            if (!mHighlightFinished)
            {
	            UpdateHighlightColor(dst * fac + src * (1.0f - fac));
            }

	        mHighlightFinished = (fac >= 1.0f);
        }

        public void UpdateDepth()
        {
            var camera = WorldFrame.Instance.ActiveCamera;
            Depth = (camera.Position - mPosition).LengthSquared;
        }

        public Vector3 GetNamePlatePosition()
        {
            if (mWorldModelName == null)
            {
	            return new Vector3(0.0f, 0.0f, 0.0f);
            }

	        return mWorldModelName.Position;
        }

        public void Remove()
        {
            WorldFrame.Instance.M2Manager.RemoveInstance(mModel.FileName, Uuid);
            WorldFrame.Instance.ClearSelection();
        }

        public string GetModelName()
        {
            return mModel.FileName;
        }
    }
}
