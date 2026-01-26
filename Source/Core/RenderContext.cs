using System;
using Verse;

namespace FogOfPawn
{
    public static class RenderContext
    {
        [ThreadStatic]
        private static bool _isRendering;
        
        [ThreadStatic]
        private static int _renderDepth;
        
        private static bool _firstRenderLogged = false;
        
        public static bool IsRendering => _isRendering && _renderDepth > 0;
        
        public static void BeginRender()
        {
            _isRendering = true;
            _renderDepth++;
            
            if (Prefs.DevMode && !_firstRenderLogged && _renderDepth == 1)
            {
                _firstRenderLogged = true;
                FogLog.Verbose("[RENDER CONTEXT] BeginRender called - UI masking is now active");
            }
        }
        
        public static void EndRender()
        {
            _renderDepth--;
            if (_renderDepth <= 0)
            {
                _renderDepth = 0;
                _isRendering = false;
            }
        }
        
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
