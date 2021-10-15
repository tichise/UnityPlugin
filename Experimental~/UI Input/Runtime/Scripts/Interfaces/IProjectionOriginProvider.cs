﻿/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.InputModule
{
    /// <summary>
    /// Provides the location of the projection origin for raycasting
    /// </summary>
    interface IProjectionOriginProvider
    {
        /// <summary>
        /// Proxy for the MonoBehaviour Update
        /// </summary>
        void Update();

        /// <summary>
        /// Call when in the MonoBehaviour Process method
        /// </summary>
        void Process();

        /// <summary>
        /// Returns the projection origin for the indicated hand
        /// </summary>
        /// <param name="isLeftHand">True if this is for a left hand</param>
        /// <returns>The projection origin</returns>
        Vector3 ProjectionOriginForHand(Hand hand);

        Quaternion CurrentRotation { get; }
        Vector3 ProjectionOriginLeft { get; }
        Vector3 ProjectionOriginRight { get; }

    }
}
