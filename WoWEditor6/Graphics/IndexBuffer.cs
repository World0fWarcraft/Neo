﻿using SharpDX.DXGI;

namespace WoWEditor6.Graphics
{
    class IndexBuffer : Buffer
    {
        public IndexBuffer(GxContext context) :
            base(context, SharpDX.Direct3D11.BindFlags.IndexBuffer)
        {

        }

        public Format IndexFormat { get; set; } = Format.R16_UInt;
    }
}
