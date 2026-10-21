// Copyright (c) 2021 Dylan Cheng (https://github.com/newlooper). All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Performance.Actions
{
    public class RelaySwap : ICubeAction
    {
        public async UniTask Perform( Step step )
        {
            var left       = step.Left;
            var right      = step.Right;
            var cubes      = GameManager.Cubes;
            var cubeSorted = Config.BlueCube;

            ////////////////////////////////
            /// Swap element positions in List
            GameManager.Cubes.Swap( left, right );

            ////////////////////
            // Bind pre-movement target positions
            var newLeft  = cubes[left].transform.position + new Vector3( Config.HorizontalGap, 0, 0 );
            var newRight = cubes[right].transform.position + new Vector3( -Config.HorizontalGap, 0, 0 );

            CodeDictionary.AddMarkLine( step.CodeLineKey );
            
            ////////////
            /// Execute animated displacement
            await UniTask.WhenAll(
                CubeController.Move( cubes[right].gameObject, new[]
                {
                    new Pace( newRight, Config.RedCube )
                } ),
                CubeController.Move( cubes[left].gameObject, new[]
                {
                    new Pace( newLeft, step.Pace.MovingMaterial )
                } )
            );

            ////////////
            /// Movement complete: revert highlight materials
            CubeController.SetPillarMaterial( cubes[left], cubeSorted );
            CubeController.SetPillarMaterial( cubes[right], cubeSorted );
            
            CodeDictionary.RemoveMarkLine( step.CodeLineKey );

            Interlocked.Increment( ref CubeController.rewindIndex );
        }
    }
}