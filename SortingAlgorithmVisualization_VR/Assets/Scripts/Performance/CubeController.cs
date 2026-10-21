// Copyright (c) 2021 Dylan Cheng (https://github.com/newlooper). All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Performance.Actions;
using Performance.ProducerConsumer;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Performance
{
    public class CubeController : MonoBehaviour
    {
        public static          Slider               speed;
        public static          bool                 inPlay;
        public static          int                  runLevel = 2;
        public static          bool                 inAction;
        public static          int                  courseIndex;
        public static          int                  rewindIndex;
        public static readonly FixedSizeQueue<Step> FixedBox = new FixedSizeQueue<Step>( 1 );
        public                 Transform            scaler;

        public int Value { get; private set; }

        private void Awake()
        {
            speed = GameObject.Find( "SliderSpeed" ).GetComponent<Slider>();
            scaler = transform.Find( "Cube" );
        }

        private void SetButtonText( string str )
        {
            var buttons = GetComponentsInChildren<Button>();
            foreach ( var btn in buttons ) btn.GetComponentInChildren<Text>().text = str;
        }

        public void SetValue( int value, bool changeScale = true )
        {
            if ( changeScale )
                scaler.localScale = new Vector3( 1, value * Config.CubeScale, 1 );
            SetButtonText( value.ToString() );
            Value = value;
        }

        public static async UniTask Play()
        {
            if ( inPlay ) return;
            inPlay = true;
            CodeDictionary.inPlay = true;
            GameManager.EnableButtons( false );

            var totalSteps = PerformanceQueue.Course.Count;
            while ( runLevel > 0 && courseIndex < totalSteps )
            {
                if ( FixedBox.Dequeue( out var step ) ) // consumer
                {
                    inAction = true;
                    Interlocked.Increment( ref courseIndex );
                    await Context.Execute( step );
                    inAction = false;
                }
                else
                {
                    await UniTask.Yield();
                }
            }

            var isFinished = courseIndex >= totalSteps && totalSteps > 0 && runLevel > 0;

            FixedBox.Dequeue( out var none );
            inPlay = false;
            CodeDictionary.inPlay = false;
            ProgressBar.SetPlayerButtonStatus( 0 );

            if ( isFinished )
            {
                await PlayCompletionEffect();
            }

            GameManager.EnableButtons( true );
        }

        public static void SetPillarMaterial( GameObject go, Material mat )
        {
            if ( go == null ) return;
            var cube = go.transform.Find( "Cube" );
            if ( cube == null ) return;
            var pillar = cube.Find( "Pillar" );
            if ( pillar == null ) return;
            var renderer = pillar.GetComponent<MeshRenderer>();
            if ( renderer != null ) renderer.material = mat;
        }

        public static async UniTask PlayCompletionEffect()
        {
            var cubes = GameManager.Cubes;
            if ( cubes == null || cubes.Count == 0 ) return;

            var defaultMat = Config.DefaultCube;
            var completedMat = Config.GreenCube;
            var isHeap = Sort.className == "Heap";

            // Phase 1: Left-to-right verification wave sweep
            for ( var i = 0; i < cubes.Count; i++ )
            {
                if ( inPlay || cubes != GameManager.Cubes || cubes[i] == null || courseIndex == 0 ) return;
                SetPillarMaterial( cubes[i], completedMat );
                if ( isHeap && CompleteBinaryTree.treeNodes != null && i < CompleteBinaryTree.treeNodes.Count && CompleteBinaryTree.treeNodes[i] != null )
                    SetPillarMaterial( CompleteBinaryTree.treeNodes[i], completedMat );
                await UniTask.Delay( TimeSpan.FromMilliseconds( 25 ) );
            }

            // Phase 2: Celebratory hold
            await UniTask.Delay( TimeSpan.FromMilliseconds( 250 ) );

            // Phase 3: Smooth return wave from left to right back to default
            for ( var i = 0; i < cubes.Count; i++ )
            {
                if ( inPlay || cubes != GameManager.Cubes || cubes[i] == null || courseIndex == 0 ) return;
                SetPillarMaterial( cubes[i], defaultMat );
                if ( isHeap && CompleteBinaryTree.treeNodes != null && i < CompleteBinaryTree.treeNodes.Count && CompleteBinaryTree.treeNodes[i] != null )
                    SetPillarMaterial( CompleteBinaryTree.treeNodes[i], defaultMat );
                await UniTask.Delay( TimeSpan.FromMilliseconds( 15 ) );
            }
        }

        public static UniTask SwapTwoObjectPosition( GameObject one, GameObject two, Pace pace )
        {
            ////////////////////
            // Bind pre-movement target positions
            var posOne = one.transform.position;
            var posTwo = two.transform.position;

            ////////////////////
            /// Concurrently animate split displacement trajectories
            return UniTask.WhenAll(
                MoveInHigh( one, posTwo, pace ),
                MoveInLow( two, posOne, pace )
            );
        }

        private static UniTask MoveInHigh( GameObject mvObj, Vector3 target, Pace pace )
        {
            return Move( mvObj, new[]
            {
                new Pace( mvObj.transform.position + new Vector3( 0, 0, Config.HorizontalGap ), pace.MovingMaterial ),
                new Pace( target + new Vector3( 0, 0, Config.HorizontalGap ), pace.MovingMaterial ),
                new Pace( target, pace.MovingMaterial )
            } );
        }

        private static UniTask MoveInLow( GameObject mvObj, Vector3 target, Pace pace )
        {
            return Move( mvObj, new[]
            {
                new Pace( mvObj.transform.position + new Vector3( 0, 0, -Config.HorizontalGap ), pace.MovingMaterial ),
                new Pace( target + new Vector3( 0, 0, -Config.HorizontalGap ), pace.MovingMaterial ),
                new Pace( target, pace.MovingMaterial )
            } );
        }

        public static async UniTask Move( GameObject from, Pace[] paces )
        {
            foreach ( var pace in paces )
            {
                if ( runLevel < 1 ) return;

                SetPillarMaterial( from, pace.MovingMaterial );

                while ( from.transform.position != pace.Target && runLevel >= 1 )
                {
                    var currentSpeed = pace.Speed == 0 ? speed.value : pace.Speed;
                    from.transform.position = Vector3.MoveTowards(
                        from.transform.position,
                        pace.Target,
                        currentSpeed * Time.deltaTime );
                    await UniTask.Yield();
                }
            }
        }

        public static async UniTask MoveAndScale( GameObject cube, Vector3 target, Vector3 targetScale, Step step )
        {
            var startPos   = cube.transform.position;
            var scaler     = cube.GetComponent<CubeController>().scaler;
            var startScale = scaler.localScale;
            var distance   = Vector3.Distance( startPos, target );
            SetPillarMaterial( cube, step.Pace.MovingMaterial );

            var i = 0f;
            while ( i < 1f && runLevel > 0 )
            {
                var rate = speed.value / distance;
                i += Time.deltaTime * rate;

                cube.transform.position = Vector3.Lerp( startPos, target, i );
                scaler.localScale = Vector3.Lerp( startScale, targetScale, i );

                await UniTask.Yield();
            }
        }
    }
}