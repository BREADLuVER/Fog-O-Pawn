using System;
using Verse;

namespace FogOfPawn
{
    /// <summary>
    /// Thread-local context that tracks whether we're currently in UI rendering phase.
    /// When IsRendering = true, data accessors return masked values.
    /// When IsRendering = false (game logic), data accessors return real values.
    /// </summary>
    public static class RenderContext
    {
        [ThreadStatic]
        private static bool _isRendering;
        
        [ThreadStatic]
        private static int _renderDepth;
        
        // Debug tracking
        private static bool _firstRenderLogged = false;
        
        /// <summary>
        /// True when we're inside UI rendering code.
        /// Data accessors should return masked values during this time.
        /// </summary>
        public static bool IsRendering => _isRendering && _renderDepth > 0;
        
        /// <summary>
        /// Call at the start of a UI rendering entry point.
        /// Supports nested calls via depth tracking.
        /// </summary>
        public static void BeginRender()
        {
            _isRendering = true;
            _renderDepth++;
            
            // Debug: Log first time render context is activated
            if (Prefs.DevMode && !_firstRenderLogged && _renderDepth == 1)
            {
                _firstRenderLogged = true;
                FogLog.Verbose("[RENDER CONTEXT] BeginRender called - UI masking is now active");
            }
        }
        
        /// <summary>
        /// Call at the end of a UI rendering entry point.
        /// Must be called in a finally block to ensure cleanup.
        /// </summary>
        public static void EndRender()
        {
            _renderDepth--;
            if (_renderDepth <= 0)
            {
                _renderDepth = 0;
                _isRendering = false;
            }
        }
        
        /// <summary>
        /// Temporarily disable rendering context for nested game logic calls.
        /// Returns an IDisposable that restores context on disposal.
        /// </summary>
        public static IDisposable SuspendForGameLogic()
        {
            return new RenderSuspension();
        }
        
        private class RenderSuspension : IDisposable
        {
            private readonly bool _wasRendering;
            private readonly int _depth;
            
            public RenderSuspension()
            {
                _wasRendering = _isRendering;
                _depth = _renderDepth;
                _isRendering = false;
                _renderDepth = 0;
            }
            
            public void Dispose()
            {
                _isRendering = _wasRendering;
                _renderDepth = _depth;
            }
        }
    }
}
