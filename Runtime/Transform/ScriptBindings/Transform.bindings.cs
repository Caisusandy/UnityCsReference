// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;

using System;
using System.Collections;

namespace UnityEngine
{
    //*undocumented
    internal enum RotationOrder { OrderXYZ, OrderXZY, OrderYZX, OrderYXZ, OrderZXY, OrderZYX }

    // Position, rotation and scale of an object.
    [NativeHeader("Configuration/UnityConfigure.h")]
    [NativeHeader("Runtime/Transform/Transform.h")]
    [NativeHeader("Runtime/Transform/ScriptBindings/TransformScriptBindings.h")]
    [RequiredByNativeCode]
    public partial class Transform : Component, IEnumerable
    {
        protected Transform() {}

        // The position of the transform in world space.
        public extern Vector3 position { get; set; }

        // Position of the transform relative to the parent transform.
        public extern Vector3 localPosition { get; set; }

        // Get local euler angles with rotation order specified
        internal extern Vector3 GetLocalEulerAngles(RotationOrder order);

        // Set local euler angles with rotation order specified
        internal extern void SetLocalEulerAngles(Vector3 euler, RotationOrder order);

        // Set local euler hint
        [NativeConditional("UNITY_EDITOR")]
        internal extern void SetLocalEulerHint(Vector3 euler);

        // The rotation as Euler angles in degrees.
        public Vector3 eulerAngles { get { return rotation.eulerAngles; } set { rotation = Quaternion.Euler(value); } }

        // The rotation as Euler angles in degrees relative to the parent transform's rotation.
        public Vector3 localEulerAngles { get { return localRotation.eulerAngles; } set { localRotation = Quaternion.Euler(value); } }

        // The red axis of the transform in world space.
        public Vector3 right { get { return rotation * Vector3.right; } set { rotation = Quaternion.FromToRotation(Vector3.right, value); } }

        // The green axis of the transform in world space.
        public Vector3 up { get { return rotation * Vector3.up; } set { rotation = Quaternion.FromToRotation(Vector3.up, value); } }

        // The blue axis of the transform in world space.
        public Vector3 forward { get { return rotation * Vector3.forward; } set { rotation = Quaternion.LookRotation(value); } }

        // The rotation of the transform in world space stored as a [[Quaternion]].
        public extern Quaternion rotation { get; set; }

        // The rotation of the transform relative to the parent transform's rotation.
        public extern Quaternion localRotation { get; set; }

        // The euler rotation order for this transform
        [NativeConditional("UNITY_EDITOR")]
        internal RotationOrder rotationOrder
        {
            get { return (RotationOrder)GetRotationOrderInternal(); }
            set { SetRotationOrderInternal(value); }
        }

        [NativeConditional("UNITY_EDITOR")]
        [NativeMethod("GetRotationOrder")]
        internal extern int GetRotationOrderInternal();
        [NativeConditional("UNITY_EDITOR")]
        [NativeMethod("SetRotationOrder")]
        internal extern void SetRotationOrderInternal(RotationOrder rotationOrder);

        // The scale of the transform relative to the parent.
        public extern Vector3 localScale { get; set; }

        // The parent of the transform.
        public Transform parent
        {
            get { return parentInternal; }
            set
            {
                if (this is RectTransform)
                    Debug.LogWarning("Parent of RectTransform is being set with parent property. Consider using the SetParent method instead, with the worldPositionStays argument set to false. This will retain local orientation and scale rather than world orientation and scale, which can prevent common UI scaling issues.", this);
                parentInternal = value;
            }
        }

        internal Transform parentInternal
        {
            get { return GetParent(); }
            set { SetParent(value); }
        }

        private extern Transform GetParent();

        public void SetParent(Transform p)
        {
            SetParent(p, true);
        }

        [FreeFunction("SetParent", HasExplicitThis = true)]
        public extern void SetParent(Transform parent, bool worldPositionStays);

        // Matrix that transforms a point from world space into local space (RO).
        public extern Matrix4x4 worldToLocalMatrix { get; }
        // Matrix that transforms a point from local space into world space (RO).
        public extern Matrix4x4 localToWorldMatrix { get; }

        // Set position and rotation in world space
        public extern void SetPositionAndRotation(Vector3 position, Quaternion rotation);

        public extern void SetLocalPositionAndRotation(Vector3 localPosition, Quaternion localRotation);

        // Moves the transform in the direction and distance of /translation/.
        public void Translate(Vector3 translation, [UnityEngine.Internal.DefaultValue("Space.Self")] Space relativeTo)
        {
            if (relativeTo == Space.World)
                position += translation;
            else
                position += TransformDirection(translation);
        }

        public void Translate(Vector3 translation)
        {
            Translate(translation, Space.Self);
        }

        // Moves the transform by /x/ along the x axis, /y/ along the y axis, and /z/ along the z axis.
        public void Translate(float x, float y, float z, [UnityEngine.Internal.DefaultValue("Space.Self")] Space relativeTo)
        {
            Translate(new Vector3(x, y, z), relativeTo);
        }

        public void Translate(float x, float y, float z)
        {
            Translate(new Vector3(x, y, z), Space.Self);
        }

        // Moves the transform in the direction and distance of /translation/.
        public void Translate(Vector3 translation, Transform relativeTo)
        {
            if (relativeTo)
                position += relativeTo.TransformDirection(translation);
            else
                position += translation;
        }

        // Moves the transform by /x/ along the x axis, /y/ along the y axis, and /z/ along the z axis.
        public void Translate(float x, float y, float z, Transform relativeTo)
        {
            Translate(new Vector3(x, y, z), relativeTo);
        }

        // Applies a rotation of /eulerAngles.z/ degrees around the z axis, /eulerAngles.x/ degrees around the x axis, and /eulerAngles.y/ degrees around the y axis (in that order).
        public void Rotate(Vector3 eulers, [UnityEngine.Internal.DefaultValue("Space.Self")] Space relativeTo)
        {
            Quaternion eulerRot = Quaternion.Euler(eulers.x, eulers.y, eulers.z);
            if (relativeTo == Space.Self)
                localRotation = localRotation * eulerRot;
            else
            {
                rotation = rotation * (Quaternion.Inverse(rotation) * eulerRot * rotation);
            }
        }

        public void Rotate(Vector3 eulers)
        {
            Rotate(eulers, Space.Self);
        }

        // Applies a rotation of /zAngle/ degrees around the z axis, /xAngle/ degrees around the x axis, and /yAngle/ degrees around the y axis (in that order).
        public void Rotate(float xAngle, float yAngle, float zAngle, [UnityEngine.Internal.DefaultValue("Space.Self")] Space relativeTo)
        {
            Rotate(new Vector3(xAngle, yAngle, zAngle), relativeTo);
        }

        public void Rotate(float xAngle, float yAngle, float zAngle)
        {
            Rotate(new Vector3(xAngle, yAngle, zAngle), Space.Self);
        }

        [NativeMethod("RotateAround")]
        internal extern void RotateAroundInternal(Vector3 axis, float angle);

        // Rotates the transform around /axis/ by /angle/ degrees.
        public void Rotate(Vector3 axis, float angle, [UnityEngine.Internal.DefaultValue("Space.Self")] Space relativeTo)
        {
            if (relativeTo == Space.Self)
                RotateAroundInternal(transform.TransformDirection(axis), angle * Mathf.Deg2Rad);
            else
                RotateAroundInternal(axis, angle * Mathf.Deg2Rad);
        }

        public void Rotate(Vector3 axis, float angle)
        {
            Rotate(axis, angle, Space.Self);
        }

        // Rotates the transform about /axis/ passing through /point/ in world coordinates by /angle/ degrees.
        public void RotateAround(Vector3 point, Vector3 axis, float angle)
        {
            Vector3 worldPos = position;
            Quaternion q = Quaternion.AngleAxis(angle, axis);
            Vector3 dif = worldPos - point;
            dif = q * dif;
            worldPos = point + dif;
            position = worldPos;
            RotateAroundInternal(axis, angle * Mathf.Deg2Rad);
        }

        // Rotates the transform so the forward vector points at /target/'s current position.
        public void LookAt(Transform target, [UnityEngine.Internal.DefaultValue("Vector3.up")] Vector3 worldUp) { if (target) LookAt(target.position, worldUp); }
        public void LookAt(Transform target) { if (target) LookAt(target.position, Vector3.up); }

        // Rotates the transform so the forward vector points at /worldPosition/.
        public void LookAt(Vector3 worldPosition, [UnityEngine.Internal.DefaultValue("Vector3.up")] Vector3 worldUp) { Internal_LookAt(worldPosition, worldUp); }
        public void LookAt(Vector3 worldPosition) { Internal_LookAt(worldPosition, Vector3.up); }

        [FreeFunction("Internal_LookAt", HasExplicitThis = true)]
        private extern void Internal_LookAt(Vector3 worldPosition, Vector3 worldUp);

        // Transforms /direction/ from local space to world space.
        public extern Vector3 TransformDirection(Vector3 direction);

        // Transforms direction /x/, /y/, /z/ from local space to world space.
        public Vector3 TransformDirection(float x, float y, float z) { return TransformDirection(new Vector3(x, y, z)); }

        // Transforms multiple directions from local space to world space.
        internal unsafe extern void TransformDirections([Span("count", isReadOnly: true)] Vector3* directions, int count, [Span("transformedCount", isReadOnly: false)] Vector3* transformedDirections, int transformedCount);
        public unsafe void TransformDirections(ReadOnlySpan<Vector3> directions, Span<Vector3> transformedDirections)
        {
            if (directions.Length != transformedDirections.Length)
                throw new InvalidOperationException($"Both spans passed to Transform.TransformDirections() must be the same length");

            fixed (Vector3* srcPtr = directions)
            {
                fixed (Vector3* destPtr = transformedDirections)
                {
                    TransformDirections(srcPtr, directions.Length, destPtr, transformedDirections.Length);
                }
            }
        }
        public unsafe void TransformDirections(Span<Vector3> directions)
        {
            TransformDirections(directions, directions);
        }


        // Transforms a /direction/ from world space to local space. The opposite of Transform.TransformDirection.
        public extern Vector3 InverseTransformDirection(Vector3 direction);

        // Transforms the direction /x/, /y/, /z/ from world space to local space. The opposite of Transform.TransformDirection.
        public Vector3 InverseTransformDirection(float x, float y, float z) { return InverseTransformDirection(new Vector3(x, y, z)); }

        // Transforms multiple directions from world space to local space. The opposite of Transform.TransformDirections.
        internal unsafe extern void InverseTransformDirections([Span("count", isReadOnly: true)] Vector3* directions, int count, [Span("transformedCount", isReadOnly: false)] Vector3* transformedDirections, int transformedCount);
        public unsafe void InverseTransformDirections(ReadOnlySpan<Vector3> directions, Span<Vector3> transformedDirections)
        {
            if (directions.Length != transformedDirections.Length)
                throw new InvalidOperationException($"Both spans passed to Transform.InverseTransformDirections() must be the same length");

            fixed (Vector3* srcPtr = directions)
            {
                fixed (Vector3* destPtr = transformedDirections)
                {
                    InverseTransformDirections(srcPtr, directions.Length, destPtr, transformedDirections.Length);
                }
            }
        }
        public unsafe void InverseTransformDirections(Span<Vector3> directions)
        {
            InverseTransformDirections(directions, directions);
        }


        // Transforms /vector/ from local space to world space.
        public extern Vector3 TransformVector(Vector3 vector);

        // Transforms vector /x/, /y/, /z/ from local space to world space.
        public Vector3 TransformVector(float x, float y, float z) { return TransformVector(new Vector3(x, y, z)); }

        // Transforms multiple vectors from local space to world space.
        internal unsafe extern void TransformVectors([Span("count", isReadOnly: true)] Vector3* vectors, int count, [Span("transformedCount", isReadOnly: false)] Vector3* transformedVectors, int transformedCount);
        public unsafe void TransformVectors(ReadOnlySpan<Vector3> vectors, Span<Vector3> transformedVectors)
        {
            if (vectors.Length != transformedVectors.Length)
                throw new InvalidOperationException($"Both spans passed to Transform.TransformVectors() must be the same length");

            fixed (Vector3* srcPtr = vectors)
            {
                fixed (Vector3* destPtr = transformedVectors)
                {
                    TransformVectors(srcPtr, vectors.Length, destPtr, transformedVectors.Length);
                }
            }
        }
        public unsafe void TransformVectors(Span<Vector3> vectors)
        {
            TransformVectors(vectors, vectors);
        }


        // Transforms a /vector/ from world space to local space. The opposite of Transform.TransformVector.
        public extern Vector3 InverseTransformVector(Vector3 vector);

        // Transforms the vector /x/, /y/, /z/ from world space to local space. The opposite of Transform.TransformVector.
        public Vector3 InverseTransformVector(float x, float y, float z) { return InverseTransformVector(new Vector3(x, y, z)); }

        // Transforms multiple vectors from world space to local space. The opposite of Transform.TransformVectors.
        internal unsafe extern void InverseTransformVectors([Span("count", isReadOnly: true)] Vector3* vectors, int count, [Span("transformedCount", isReadOnly: false)] Vector3* transformedVectors, int transformedCount);
        public unsafe void InverseTransformVectors(ReadOnlySpan<Vector3> vectors, Span<Vector3> transformedVectors)
        {
            if (vectors.Length != transformedVectors.Length)
                throw new InvalidOperationException($"Both spans passed to Transform.InverseTransformVectors() must be the same length");

            fixed (Vector3* srcPtr = vectors)
            {
                fixed (Vector3* destPtr = transformedVectors)
                {
                    InverseTransformVectors(srcPtr, vectors.Length, destPtr, transformedVectors.Length);
                }
            }
        }
        public unsafe void InverseTransformVectors(Span<Vector3> vectors)
        {
            InverseTransformVectors(vectors, vectors);
        }


        // Transforms /position/ from local space to world space.
        public extern Vector3 TransformPoint(Vector3 position);

        // Transforms the position /x/, /y/, /z/ from local space to world space.
        public Vector3 TransformPoint(float x, float y, float z) { return TransformPoint(new Vector3(x, y, z)); }

        // Transforms multiple positions from local space to world space.
        internal unsafe extern void TransformPoints([Span("count", isReadOnly: true)] Vector3* positions, int count, [Span("transformedCount", isReadOnly: false)] Vector3* transformedPositions, int transformedCount);
        public unsafe void TransformPoints(ReadOnlySpan<Vector3> positions, Span<Vector3> transformedPositions)
        {
            if (positions.Length != transformedPositions.Length)
                throw new InvalidOperationException($"Both spans passed to Transform.TransformPoints() must be the same length");

            fixed (Vector3* srcPtr = positions)
            {
                fixed (Vector3* destPtr = transformedPositions)
                {
                    TransformPoints(srcPtr, positions.Length, destPtr, transformedPositions.Length);
                }
            }
        }
        public unsafe void TransformPoints(Span<Vector3> positions)
        {
            TransformPoints(positions, positions);
        }


        // Transforms /position/ from world space to local space. The opposite of Transform.TransformPoint.
        public extern Vector3 InverseTransformPoint(Vector3 position);

        // Transforms the position /x/, /y/, /z/ from world space to local space. The opposite of Transform.TransformPoint.
        public Vector3 InverseTransformPoint(float x, float y, float z) { return InverseTransformPoint(new Vector3(x, y, z)); }

        // Transforms multiple positions from world space to local space. The opposite of Transform.TransformPoints.
        internal unsafe extern void InverseTransformPoints([Span("count", isReadOnly: true)] Vector3* positions, int count, [Span("transformedCount", isReadOnly: false)] Vector3* transformedPositions, int transformedCount);
        public unsafe void InverseTransformPoints(ReadOnlySpan<Vector3> positions, Span<Vector3> transformedPositions)
        {
            if (positions.Length != transformedPositions.Length)
                throw new InvalidOperationException($"Both spans passed to Transform.InverseTransformPoints() must be the same length");

            fixed (Vector3* srcPtr = positions)
            {
                fixed (Vector3* destPtr = transformedPositions)
                {
                    InverseTransformPoints(srcPtr, positions.Length, destPtr, transformedPositions.Length);
                }
            }
        }
        public unsafe void InverseTransformPoints(Span<Vector3> positions)
        {
            InverseTransformPoints(positions, positions);
        }


        // Returns the topmost transform in the hierarchy.
        public Transform root { get { return GetRoot(); } }

        private extern Transform GetRoot();

        // The number of children the Transform has.
        public extern int childCount
        {
            [NativeMethod("GetChildrenCount")]
            get;
        }

        // Unparents all children.
        [FreeFunction("DetachChildren", HasExplicitThis = true)]
        public extern void DetachChildren();

        // Move itself to the end of the parent's array of children
        public extern void SetAsFirstSibling();

        // Move itself to the beginning of the parent's array of children
        public extern void SetAsLastSibling();

        public extern void SetSiblingIndex(int index);

        [NativeMethod("MoveAfterSiblingInternal")]
        internal extern void MoveAfterSibling(Transform transform, bool notifyEditorAndMarkDirty);

        public extern int GetSiblingIndex();

        [FreeFunction]
        private static extern Transform FindRelativeTransformWithPath([NotNull("NullExceptionObject")] Transform transform, string path, [UnityEngine.Internal.DefaultValue("false")] bool isActiveOnly);

        // Finds a child by /name/ and returns it.
        public Transform Find(string n)
        {
            if (n == null)
                throw new ArgumentNullException("Name cannot be null");
            return FindRelativeTransformWithPath(this, n, false);
        }

        //*undocumented
        [NativeConditional("UNITY_EDITOR")]
        internal extern void SendTransformChangedScale();

        // The global scale of the object (RO).
        public extern Vector3 lossyScale
        {
            [NativeMethod("GetWorldScaleLossy")]
            get;
        }

        // Is this transform a child of /parent/?
        [FreeFunction("Internal_IsChildOrSameTransform", HasExplicitThis = true)]
        public extern bool IsChildOf([NotNull] Transform parent);

        // Has the transform changed since the last time the flag was set to 'false'?
        [NativeProperty("HasChangedDeprecated")]
        public extern bool hasChanged { get; set; }

        //*undocumented*
        [Obsolete("FindChild has been deprecated. Use Find instead (UnityUpgradable) -> Find([mscorlib] System.String)", false)]
        public Transform FindChild(string n) { return Find(n); }

        //*undocumented* Documented separately
        public IEnumerator GetEnumerator()
        {
            return new Transform.Enumerator(this);
        }

        private class Enumerator : IEnumerator
        {
            Transform outer;
            int currentIndex = -1;

            internal Enumerator(Transform outer)
            {
                this.outer = outer;
            }

            //*undocumented*
            public object Current
            {
                get { return outer.GetChild(currentIndex); }
            }

            //*undocumented*
            public bool MoveNext()
            {
                int childCount = outer.childCount;
                return ++currentIndex < childCount;
            }

            //*undocumented*
            public void Reset() { currentIndex = -1; }
        }

        // *undocumented* DEPRECATED
        [Obsolete("warning use Transform.Rotate instead.")]
        public extern void RotateAround(Vector3 axis, float angle);

        // *undocumented* DEPRECATED
        [Obsolete("warning use Transform.Rotate instead.")]
        public extern void RotateAroundLocal(Vector3 axis, float angle);

        // Get a transform child by index
        [NativeThrows]
        [FreeFunction("GetChild", HasExplicitThis = true)]
        public extern Transform GetChild(int index);

        //*undocumented* DEPRECATED
        [Obsolete("warning use Transform.childCount instead (UnityUpgradable) -> Transform.childCount", false)]
        [NativeMethod("GetChildrenCount")]
        public extern int GetChildCount();

        public int hierarchyCapacity
        {
            get { return internal_getHierarchyCapacity(); }
            set { internal_setHierarchyCapacity(value); }
        }

        [FreeFunction("GetHierarchyCapacity", HasExplicitThis = true)]
        private extern int internal_getHierarchyCapacity();

        [FreeFunction("SetHierarchyCapacity", HasExplicitThis = true)]
        private extern void internal_setHierarchyCapacity(int value);

        public int hierarchyCount { get { return internal_getHierarchyCount(); } }

        [FreeFunction("GetHierarchyCount", HasExplicitThis = true)]
        private extern int internal_getHierarchyCount();

        [NativeConditional("UNITY_EDITOR")]
        [FreeFunction("IsNonUniformScaleTransform", HasExplicitThis = true)]
        internal extern bool IsNonUniformScaleTransform();

        [NativeConditional("UNITY_EDITOR")]
        internal bool constrainProportionsScale
        {
            get => IsConstrainProportionsScale();
            set => SetConstrainProportionsScale(value);
        }

        [NativeConditional("UNITY_EDITOR")]
        private extern void SetConstrainProportionsScale(bool isLinked);

        [NativeConditional("UNITY_EDITOR")]
        private extern bool IsConstrainProportionsScale();
    }
}
