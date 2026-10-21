// Copyright (c) 2021 Dylan Cheng (https://github.com/newlooper). All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Performance
{
    public class CodeDictionary : MonoBehaviour
    {
        public static           bool                                           inPlay     = false;
        private static readonly List<string>                                   MarkLines  = new List<string>();
        private static readonly Regex                                          MarksRegex = new Regex( @"</?mark[^>]*>" );
        private static          List<string>                                   _keys;
        private static          Dictionary<string, string>                     _codeLines;
        private static          Dictionary<string, Dictionary<string, string>> _codeDict;

        // Scrolling state
        private RectTransform   _rectTransform;
        private RectTransform   _panelRectTransform;
        private TextMeshProUGUI _textMeshPro;
        private float           _scrollOffset = 0f;
        private float           _targetScroll = 0f;
        private float           _initialY     = -13f;
        private string          _lastAlg      = "";
        private const float     VisibleHeight = 305f;

        // Stable algorithm identifiers matching Dropdown option indices 0..11
        private static readonly string[] AlgorithmKeys = new[]
        {
            "Bubble",
            "Cocktail",
            "Comb",
            "Gnome",
            "Heap",
            "Insertion",
            "MergeBottomUp",
            "OddEven",
            "Quick",
            "Radix",
            "Selection",
            "Shell"
        };

        public static string GetCurrentAlgorithmName()
        {
            // Try resolving by stable Dropdown index first (immune to localization changes)
            var algObj = GameObject.Find( "Algorithm" );
            if ( algObj != null )
            {
                var dropdown = algObj.GetComponent<Dropdown>();
                if ( dropdown != null && dropdown.value >= 0 && dropdown.value < AlgorithmKeys.Length )
                {
                    return AlgorithmKeys[dropdown.value];
                }
            }

            if ( !string.IsNullOrEmpty( Sort.className ) )
            {
                return Sort.className;
            }

            return "Bubble";
        }

        private void Awake()
        {
            EnsureLoaded();
            _rectTransform = GetComponent<RectTransform>();
            _textMeshPro   = GetComponent<TextMeshProUGUI>();
            if ( transform.parent != null )
            {
                _panelRectTransform = transform.parent as RectTransform;
            }
            if ( _rectTransform != null )
            {
                _initialY = _rectTransform.anchoredPosition.y;
            }
        }

        private void Start()
        {
            EnsureLoaded();
            if ( _textMeshPro == null ) _textMeshPro = GetComponent<TextMeshProUGUI>();
            var currentAlg = !string.IsNullOrEmpty( Sort.className ) ? Sort.className : GetCurrentAlgorithmName();
            if ( _codeDict != null && _codeDict.ContainsKey( currentAlg ) && _textMeshPro != null )
            {
                _textMeshPro.text = GetText( _codeDict[currentAlg], true );
            }
        }

        private static void EnsureLoaded()
        {
            if ( _codeDict != null ) return;
            try
            {
                var json = Resources.Load<TextAsset>( "Json/CodeLines" );
                if ( json != null )
                {
                    _codeDict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>( json.text );
                }
                else
                {
                    Debug.LogWarning( "[CodeDictionary] Resources/Json/CodeLines asset not found." );
                }
            }
            catch ( System.Exception ex )
            {
                Debug.LogError( "[CodeDictionary] Failed to deserialize CodeLines JSON: " + ex );
            }
        }

        private void Update()
        {
            EnsureLoaded();
            if ( _codeDict == null ) return;

            var currentAlg = !string.IsNullOrEmpty( Sort.className ) ? Sort.className : GetCurrentAlgorithmName();

            // Reset scroll when algorithm changes
            if ( currentAlg != _lastAlg )
            {
                _lastAlg      = currentAlg;
                _targetScroll = 0f;
                _scrollOffset = 0f;
            }

            if ( inPlay )
            {
                if ( !_codeDict.ContainsKey( currentAlg ) ) return;

                _codeLines = _codeDict[currentAlg];
                _keys = new List<string>( _codeLines.Keys );
                foreach ( var key in _keys )
                {
                    if ( MarkLines.Contains( key ) )
                    {
                        if ( !_codeLines[key].StartsWith( "<mark" ) )
                        {
                            switch ( key )
                            {
                                case "Swap":
                                case "Swap2":
                                case "Copy":
                                case "RadixPick":
                                    _codeLines[key] = "<mark=#00e67655>" + _codeLines[key] + "</mark>";
                                    break;
                                case "Selected":
                                case "Selected2":
                                case "Selected3":
                                case "Selected4":
                                case "Compare":
                                    _codeLines[key] = "<mark=#ffc10755>" + _codeLines[key] + "</mark>";
                                    break;
                                default:
                                    _codeLines[key] = "<mark=#00e5ff33>" + _codeLines[key] + "</mark>";
                                    break;
                            }
                        }
                    }
                    else
                    {
                        if ( _codeLines[key].StartsWith( "<mark" ) )
                            _codeLines[key] = MarksRegex.Replace( _codeLines[key], "" );
                    }
                }

                if ( _textMeshPro != null )
                {
                    _textMeshPro.text = GetText( _codeLines, false );
                }
            }
            else
            {
                if ( MarkLines.Count > 0 )
                {
                    MarkLines.Clear();
                }

                if ( _codeDict.ContainsKey( currentAlg ) && _textMeshPro != null )
                {
                    _textMeshPro.text = GetText( _codeDict[currentAlg], true );
                }
            }

            UpdateScrolling();
        }

        private void UpdateScrolling()
        {
            if ( _rectTransform == null || _textMeshPro == null ) return;

            var textHeight = _textMeshPro.preferredHeight;
            var maxScroll  = Mathf.Max( 0f, textHeight - VisibleHeight );

            if ( !inPlay )
            {
                // Mouse wheel scrolling when cursor is over inspector panel
                var mousePos = Input.mousePosition;
                if ( _panelRectTransform != null &&
                     RectTransformUtility.RectangleContainsScreenPoint( _panelRectTransform, mousePos ) )
                {
                    var scrollDelta = Input.mouseScrollDelta.y;
                    if ( Mathf.Abs( scrollDelta ) > 0.01f )
                    {
                        _targetScroll = Mathf.Clamp( _targetScroll - scrollDelta * 28f, 0f, maxScroll );
                    }
                }

                _scrollOffset = Mathf.MoveTowards( _scrollOffset, _targetScroll, Time.unscaledDeltaTime * 600f );
            }
            else
            {
                // Auto-scroll during playback to track active highlighted line
                if ( MarkLines.Count > 0 && _keys != null && _keys.Count > 0 && maxScroll > 0f )
                {
                    var activeKey = MarkLines[MarkLines.Count - 1];
                    var keyIndex  = _keys.IndexOf( activeKey );
                    if ( keyIndex >= 0 )
                    {
                        var lineRatio = (float)keyIndex / _keys.Count;
                        _targetScroll = Mathf.Clamp( lineRatio * textHeight - VisibleHeight * 0.4f, 0f, maxScroll );
                    }
                }

                _scrollOffset = Mathf.Lerp( _scrollOffset, _targetScroll, Time.unscaledDeltaTime * 8f );
            }

            _scrollOffset = Mathf.Clamp( _scrollOffset, 0f, maxScroll );
            _rectTransform.anchoredPosition = new Vector2( _rectTransform.anchoredPosition.x, _initialY + _scrollOffset );
        }

        public static void AddMarkLine( string key )
        {
            MarkLines.Add( key );
        }

        public static void RemoveMarkLine( string key )
        {
            MarkLines.Remove( key );
        }

        public static void ClearMarks()
        {
            MarkLines.Clear();
        }

        private static string GetText( Dictionary<string, string> lines, bool stripMarks = false )
        {
            var str = "";
            foreach ( var rawLine in lines.Values )
            {
                var line = stripMarks ? MarksRegex.Replace( rawLine, "" ) : rawLine;

                // Display-only formatting: compact 3-space indentation
                line = line.Replace( "\t", "   " );

                // Modern readable dark-editor palette
                line = line.Replace( "<color=blue>", "<color=#569CD6>" );
                line = line.Replace( "<color=red>", "<color=#B5CEA8>" );
                line = line.Replace( "<color=#00ffffff>", "<color=#9CDCFE>" );

                str += line + "\r\n";
            }

            return str;
        }
    }
}