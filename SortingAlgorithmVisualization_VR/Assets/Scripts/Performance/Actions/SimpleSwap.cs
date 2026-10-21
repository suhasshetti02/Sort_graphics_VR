// Copyright (c) 2021 Dylan Cheng (https://github.com/newlooper). All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System.Threading;
using Cysharp.Threading.Tasks;

namespace Performance.Actions
{
    public class SimpleSwap : ICubeAction
    {
        public async UniTask Perform( Step step )
        {
            var cubes       = GameManager.Cubes;
            var cubeDefault = Config.DefaultCube;
            var left        = step.Left;
            var right       = step.Right;

            ////////////////////////////////
            /// Swap element positions in List
            GameManager.Cubes.Swap( left, right );
            
            CodeDictionary.AddMarkLine( step.CodeLineKey );

            ////////////////////
            /// Execute animated displacement
            await CubeController.SwapTwoObjectPosition( cubes[left], cubes[right], step.Pace );

            ////////////
            /// Movement complete: revert highlight materials
            CubeController.SetPillarMaterial( cubes[left], cubeDefault );
            CubeController.SetPillarMaterial( cubes[right], cubeDefault );
            
            CodeDictionary.RemoveMarkLine( step.CodeLineKey );

            Interlocked.Increment( ref CubeController.rewindIndex );
        }
    }
}