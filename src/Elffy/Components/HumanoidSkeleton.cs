﻿#nullable enable

using Elffy.Core;

namespace Elffy.Components
{
    public class HumanoidSkeleton : Skeleton
    {
        public override void OnAttached(ComponentOwner owner) => OnAttachedCore<HumanoidSkeleton>(owner);

        public override void OnDetached(ComponentOwner owner) => OnDetachedCore<HumanoidSkeleton>(owner);
    }

    public enum HumanoidBone : byte
    {
        // Body Bones
        UpperChest = 0,
        Chest,
        Spine,
        Hips,
        Shoulder,
        UpperArm,
        LowerArm,
        Hand,
        UpperLeg,
        LowerLeg,
        Foot,
        Toes,

        // Head Bones
        RightEye,
        LeftEye,
        Head,
        Jaw,
        Neck,

        // Hand Bones
        ThumbProximal,
        ThumbIntermediate,
        ThumbDistal,
        IndexProximal,
        IndexIntermediate,
        IndexDistal,
        MiddleProximal,
        MiddleIntermediate,
        MiddleDistal,
        RingProximal,
        RingIntermediate,
        RingDistal,
        LittleProximal,
        LittleIntermediate,
        LittleDistal,
        Proximal,
        Intermediate,
        Distal,
    }
}