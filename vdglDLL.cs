using System;
using System.Runtime.InteropServices;
using VectorDraw.Geometry;
using VectorDraw.WinMessages;

namespace VectorDraw.Render
{
    internal static class vdglDLL
    {
        private static IntPtr vdrawglModule = IntPtr.Zero;
        private static vdglDLL.SetProjectionMatrix_dll_delegate SetProjectionMatrix_dll;
        private static vdglDLL.SetModelMatrix_dll_delegate SetModelMatrix_dll;
        private static vdglDLL.Status_delegate Status;

        public static void SetDefault()
        {
            vdglDLL.vdrawglModule = VectorDraw.Serialize.Activator.DynamicLoadModule("vdrawgl");
            vdglDLL.SetProjectionMatrix_dll = (vdglDLL.SetProjectionMatrix_dll_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "SetProjectionMatrix"), typeof(vdglDLL.SetProjectionMatrix_dll_delegate));
            vdglDLL.SetModelMatrix_dll = (vdglDLL.SetModelMatrix_dll_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "SetModelMatrix"), typeof(vdglDLL.SetModelMatrix_dll_delegate));
            vdglDLL.Status = (vdglDLL.Status_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "Status"), typeof(vdglDLL.Status_delegate));
            vdgl.WrapImage = new vdglTypes.WrapImage_delegate(vdglDLL.WrapImage);
            vdgl.WrapBuffer = new vdglTypes.WrapBuffer_delegate(vdglDLL.WrapBuffer);
            vdgl.SetProjectionMatrix = new vdglTypes.SetProjectionMatrix_delegate(vdglDLL.SetProjectionMatrix);
            vdgl.SetModelMatrix = new vdglTypes.SetModelMatrix_delegate(vdglDLL.SetModelMatrix);
            vdgl.SetViewport = (vdglTypes.SetViewport_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "SetViewport"), typeof(vdglTypes.SetViewport_delegate));
            vdgl.MarkListForUpdate = new vdglTypes.MarkListForUpdate_delegate(vdglDLL.MarkListForUpdate);
            vdgl.IsListNeedUpdate = new vdglTypes.IsListNeedUpdate_delegate(vdglDLL.IsListNeedUpdate);
            vdgl.GetMemoryAllocs = new vdglTypes.GetMemoryAllocs_delegate(vdglDLL.GetMemoryAllocs);
            vdgl.FlushDrawBuffers = (vdglTypes.FlushDrawBuffers_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "FlushDrawBuffers"), typeof(vdglTypes.FlushDrawBuffers_delegate));
            vdgl.GetListStatus = (vdglTypes.GetListStatus_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "GetListStatus"), typeof(vdglTypes.GetListStatus_delegate));
            vdgl.SetFunctionOverride = (vdglTypes.SetFunctionOverride_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "SetFunctionOverride"), typeof(vdglTypes.SetFunctionOverride_delegate));
            vdgl.PushObjectId = (vdglTypes.PushObjectId_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "PushObjectId"), typeof(vdglTypes.PushObjectId_delegate));
            vdgl.PopObjectId = (vdglTypes.PopObjectId_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "PopObjectId"), typeof(vdglTypes.PopObjectId_delegate));
            vdgl.CreateContext = (vdglTypes.CreateContext_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "CreateContext"), typeof(vdglTypes.CreateContext_delegate));
            vdgl.DeleteContext = (vdglTypes.DeleteContext_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "DeleteContext"), typeof(vdglTypes.DeleteContext_delegate));
            vdgl.SetBitmapContext = (vdglTypes.SetBitmapContext_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "SetBitmapContext"), typeof(vdglTypes.SetBitmapContext_delegate));
            vdgl.Finish = (vdglTypes.Finish_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "Finish"), typeof(vdglTypes.Finish_delegate));
            vdgl.ApplyFilter = (vdglTypes.ApplyFilter_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "ApplyFilter"), typeof(vdglTypes.ApplyFilter_delegate));
            vdgl.GetPropertyValue = (vdglTypes.GetPropertyValue_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "GetPropertyValue"), typeof(vdglTypes.GetPropertyValue_delegate));
            vdgl.SetPropertyValue = (vdglTypes.SetPropertyValue_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "SetPropertyValue"), typeof(vdglTypes.SetPropertyValue_delegate));
            vdgl.ClearContext = (vdglTypes.ClearContext_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "ClearContext"), typeof(vdglTypes.ClearContext_delegate));
            vdgl.BindPattern = (vdglTypes.BindPattern_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "BindPattern"), typeof(vdglTypes.BindPattern_delegate));
            vdgl.AddPatternLine = (vdglTypes.AddPatternLine_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "AddPatternLine"), typeof(vdglTypes.AddPatternLine_delegate));
            vdgl.DeletePattern = (vdglTypes.DeletePattern_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "DeletePattern"), typeof(vdglTypes.DeletePattern_delegate));
            vdgl.BindLineType = (vdglTypes.BindLineType_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "BindLineType"), typeof(vdglTypes.BindLineType_delegate));
            vdgl.AddLineTypeSegment = (vdglTypes.AddLineTypeSegment_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "AddLineTypeSegment"), typeof(vdglTypes.AddLineTypeSegment_delegate));
            vdgl.DeleteLineType = (vdglTypes.DeleteLineType_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "DeleteLineType"), typeof(vdglTypes.DeleteLineType_delegate));
            vdgl.SetMaterial = (vdglTypes.SetMaterial_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "SetMaterial"), typeof(vdglTypes.SetMaterial_delegate));
            vdgl.SetFadeEffect = (vdglTypes.SetFadeEffect_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "SetFadeEffect"), typeof(vdglTypes.SetFadeEffect_delegate));
            vdgl.SetLightProps = (vdglTypes.SetLightProps_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "SetLightProps"), typeof(vdglTypes.SetLightProps_delegate));
            vdgl.SetSectionProps = (vdglTypes.SetSectionProps_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "SetSectionProps"), typeof(vdglTypes.SetSectionProps_delegate));
            vdgl.Element_String_Init = (vdglTypes.Element_String_Init_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "Element_String_Init"), typeof(vdglTypes.Element_String_Init_delegate));
            vdgl.Element_String_AddChar = (vdglTypes.Element_String_AddChar_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "Element_String_AddChar"), typeof(vdglTypes.Element_String_AddChar_delegate));
            vdgl.Element_String_Draw = (vdglTypes.Element_String_Draw_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "Element_String_Draw"), typeof(vdglTypes.Element_String_Draw_delegate));
            vdgl.AddOpenGLListId = (vdglTypes.AddOpenGLListId_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "AddOpenGLListId"), typeof(vdglTypes.AddOpenGLListId_delegate));
            vdgl.PFaceDraw = (vdglTypes.PFaceDraw_delegate)null;
            vdgl.ElementInit = (vdglTypes.ElementInit_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "ElementInit"), typeof(vdglTypes.ElementInit_delegate));
            vdgl.ElementSetPattern = (vdglTypes.ElementSetPattern_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "ElementSetPattern"), typeof(vdglTypes.ElementSetPattern_delegate));
            vdgl.ElementDraw = (vdglTypes.ElementDraw_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "ElementDraw"), typeof(vdglTypes.ElementDraw_delegate));
            vdgl.ElementDrawMesh = (vdglTypes.ElementDrawMesh_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "ElementDrawMesh"), typeof(vdglTypes.ElementDrawMesh_delegate));
            vdgl.ElementSetNormal = (vdglTypes.ElementSetNormal_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "ElementSetNormal"), typeof(vdglTypes.ElementSetNormal_delegate));
            vdgl.ElementSetVertex = (vdglTypes.ElementSetVertex_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "ElementSetVertex"), typeof(vdglTypes.ElementSetVertex_delegate));
            vdgl.ElementSetTexture = (vdglTypes.ElementSetTexture_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "ElementSetTexture"), typeof(vdglTypes.ElementSetTexture_delegate));
            vdgl.ElementSetColor = (vdglTypes.ElementSetColor_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "ElementSetColor"), typeof(vdglTypes.ElementSetColor_delegate));
            vdgl.CreateList = (vdglTypes.CreateList_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "CreateList"), typeof(vdglTypes.CreateList_delegate));
            vdgl.StartNewList = (vdglTypes.StartNewList_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "StartNewList"), typeof(vdglTypes.StartNewList_delegate));
            vdgl.FinishList = (vdglTypes.FinishList_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "FinishList"), typeof(vdglTypes.FinishList_delegate));
            vdgl.EmptyList = (vdglTypes.EmptyList_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "EmptyList"), typeof(vdglTypes.EmptyList_delegate));
            vdgl.IsEmptyList = (vdglTypes.IsEmptyList_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "IsEmptyList"), typeof(vdglTypes.IsEmptyList_delegate));
            vdgl.SetListFlag = (vdglTypes.SetListFlag_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "SetListFlag"), typeof(vdglTypes.SetListFlag_delegate));
            vdgl.GetListFlag = (vdglTypes.GetListFlag_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "GetListFlag"), typeof(vdglTypes.GetListFlag_delegate));
            vdgl.DrawList = (vdglTypes.DrawList_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "DrawList"), typeof(vdglTypes.DrawList_delegate));
            vdgl.ReadBuffer = (vdglTypes.ReadBuffer_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "ReadBuffer"), typeof(vdglTypes.ReadBuffer_delegate));
            vdgl.GetIdAtPixel = (vdglTypes.GetIdAtPixel_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "GetIdAtPixel"), typeof(vdglTypes.GetIdAtPixel_delegate));
            vdgl.GetDepthAtPixel = (vdglTypes.GetDepthAtPixel_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "GetDepthAtPixel"), typeof(vdglTypes.GetDepthAtPixel_delegate));
            vdgl.SetPickMode = (vdglTypes.SetPickMode_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "SetPickMode"), typeof(vdglTypes.SetPickMode_delegate));
            vdgl.ContainsPoint = (vdglTypes.ContainsPoint_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "ContainsPoint"), typeof(vdglTypes.ContainsPoint_delegate));
            vdgl.SetClipBox = (vdglTypes.SetClipBox_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "SetClipBox"), typeof(vdglTypes.SetClipBox_delegate));
            vdgl.QuickDraw2dPixelLine = (vdglTypes.QuickDraw2dPixelLine_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "QuickDraw2dPixelLine"), typeof(vdglTypes.QuickDraw2dPixelLine_delegate));
            vdgl.DrawPixelCross = (vdglTypes.DrawPixelCross_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "DrawPixelCross"), typeof(vdglTypes.DrawPixelCross_delegate));
            vdgl.QuickDraw2dPixelBox = (vdglTypes.QuickDraw2dPixelBox_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "QuickDraw2dPixelBox"), typeof(vdglTypes.QuickDraw2dPixelBox_delegate));
            vdgl.DrawPixelLine = (vdglTypes.DrawPixelLine_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "DrawPixelLine"), typeof(vdglTypes.DrawPixelLine_delegate));
            vdgl.DrawPixelBox = (vdglTypes.DrawPixelBox_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "DrawPixelBox"), typeof(vdglTypes.DrawPixelBox_delegate));
            vdgl.TestClipBox = (vdglTypes.TestClipBox_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "TestClipBox"), typeof(vdglTypes.TestClipBox_delegate));
            vdgl.ApplyGradient = (vdglTypes.ApplyGradient_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "ApplyGradient"), typeof(vdglTypes.ApplyGradient_delegate));
            vdgl.PushClipPolygon = (vdglTypes.PushClipPolygon_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "PushClipPolygon"), typeof(vdglTypes.PushClipPolygon_delegate));
            vdgl.PopClipPolygon = (vdglTypes.PopClipPolygon_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "PopClipPolygon"), typeof(vdglTypes.PopClipPolygon_delegate));
            vdgl.PushAlignToView = (vdglTypes.PushAlignToView_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "PushAlignToView"), typeof(vdglTypes.PushAlignToView_delegate));
            vdgl.PopAlignToView = (vdglTypes.PopAlignToView_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "PopAlignToView"), typeof(vdglTypes.PopAlignToView_delegate));
            vdgl.DisableClipId = (vdglTypes.DisableClipId_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "DisableClipId"), typeof(vdglTypes.DisableClipId_delegate));
            vdgl.SetActiveId = (vdglTypes.SetActiveId_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "SetActiveId"), typeof(vdglTypes.SetActiveId_delegate));
            vdgl.PolygonModeOverWrite = (vdglTypes.PolygonModeOverWrite_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "PolygonModeOverWrite"), typeof(vdglTypes.PolygonModeOverWrite_delegate));
            vdgl.LockGL = (vdglTypes.LockGL_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "LockGL"), typeof(vdglTypes.LockGL_delegate));
            vdgl.UnLockGL = (vdglTypes.UnLockGL_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "UnLockGL"), typeof(vdglTypes.UnLockGL_delegate));
            vdgl.CreateSectionCoverFacesList = (vdglTypes.CreateSectionCoverFacesList_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "CreateSectionCoverFacesList"), typeof(vdglTypes.CreateSectionCoverFacesList_delegate));
            vdgl.DeleteList = (vdglTypes.DeleteList_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "DeleteList"), typeof(vdglTypes.DeleteList_delegate));
            vdgl.BindImage = (vdglTypes.BindImage_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "BindImage"), typeof(vdglTypes.BindImage_delegate));
            vdgl.DeleteImage = (vdglTypes.DeleteImage_delegate)Marshal.GetDelegateForFunctionPointer(Kernel32.GetProcAddress(vdglDLL.vdrawglModule, "DeleteImage"), typeof(vdglTypes.DeleteImage_delegate));
        }

        public static vdglTypes.IImageWrapper WrapImage(IntPtr image) => (vdglTypes.IImageWrapper)new vdglDLL.ImageWrapper(image);

        public static vdglTypes.IBufferWrapper WrapBuffer(IntPtr buffer) => (vdglTypes.IBufferWrapper)new vdglDLL.BufferWrapper(buffer);

        public static void SetProjectionMatrix(IntPtr vdrawGlContext, Matrix m) => vdglDLL.SetProjectionMatrix_dll(vdrawGlContext, m.A00, m.A10, m.A20, m.A30, m.A01, m.A11, m.A21, m.A31, m.A02, m.A12, m.A22, m.A32, m.A03, m.A13, m.A23, m.A33);

        public static void SetModelMatrix(IntPtr vdrawGlContext, Matrix m) => vdglDLL.SetModelMatrix_dll(vdrawGlContext, m.A00, m.A10, m.A20, m.A30, m.A01, m.A11, m.A21, m.A31, m.A02, m.A12, m.A22, m.A32, m.A03, m.A13, m.A23, m.A33);

        public static void MarkListForUpdate(IntPtr list) => vdgl.SetListFlag(list, (byte)1);

        public static bool IsListNeedUpdate(IntPtr list) => vdgl.GetListFlag(list) == 1;

        public static long GetMemoryAllocs()
        {
            vdglDLL._STATUS status = new vdglDLL._STATUS();
            vdglDLL.Status(IntPtr.Zero, ref status);
            return status.AllocBytes.ToInt64();
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        private struct DRAWBUFFER
        {
            public int type;
            public IntPtr context;
            public int curitem;
            public IntPtr buffer;
            public IntPtr curimage;
            public int imageflag;
            public double width;
            public int activeid;
            public int enableactiveid;
            public int containscolors;
            public vdglTypes.Colorflag colorflag;
            public vdglTypes.drawBufferFlag drawFlag;
            public int textureon;
            public int lighting;
            public int depthon;
            public int depthWriteOn;
            public int highlighton;
            public int polygonmode;
            public int coloron;
            public int transparentorder;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        private struct IMAGE_STRUCT
        {
            public short bindType;
            public byte InterpolationMethod;
            public int width;
            public int height;
            public int stridewidth;
            public IntPtr bytes;
        }

        private class ImageWrapper : vdglTypes.IImageWrapper
        {
            private IntPtr imageptr;
            private vdglDLL.IMAGE_STRUCT imageStruct;
            private unsafe byte* imagebytes;

            public unsafe ImageWrapper(IntPtr image)
            {
                this.imageptr = image;
                if (!(image != IntPtr.Zero))
                    return;
                this.imageStruct = (vdglDLL.IMAGE_STRUCT)Marshal.PtrToStructure(image, typeof(vdglDLL.IMAGE_STRUCT));
                this.imagebytes = (byte*)this.imageStruct.bytes.ToPointer();
            }

            public short bindType => this.imageStruct.bindType;

            public byte InterpolationMethod => this.imageStruct.InterpolationMethod;

            public int width => this.imageStruct.width;

            public int height => this.imageStruct.height;

            public int stridewidth => this.imageStruct.stridewidth;

            public unsafe bool IsNull => (IntPtr)this.imagebytes == IntPtr.Zero;

            public IntPtr ToIntPtr => this.imageptr;

            public unsafe void CopyTo(IntPtr dst)
            {
                byte* pointer = (byte*)dst.ToPointer();
                byte* imagebytes = this.imagebytes;
                int index1 = 0;
                for (int index2 = this.imageStruct.height - 1; index2 >= 0; --index2)
                {
                    for (int index3 = 0; index3 < this.imageStruct.stridewidth; ++index3)
                    {
                        pointer[index1] = imagebytes[index2 * this.imageStruct.stridewidth + index3];
                        ++index1;
                    }
                }
            }
        }

        private class BufferWrapper : vdglTypes.IBufferWrapper
        {
            private vdglDLL.DRAWBUFFER BufferStruct;
            private vdglTypes.IImageWrapper image;
            private unsafe double* buf;

            public unsafe BufferWrapper(IntPtr buffer)
            {
                this.BufferStruct = (vdglDLL.DRAWBUFFER)Marshal.PtrToStructure(buffer, typeof(vdglDLL.DRAWBUFFER));
                this.buf = (double*)this.BufferStruct.buffer.ToPointer();
                this.image = (vdglTypes.IImageWrapper)new vdglDLL.ImageWrapper(this.BufferStruct.curimage);
            }

            public int type => this.BufferStruct.type;

            public object context => (object)this.BufferStruct.context;

            public int curitem => this.BufferStruct.curitem;

            public vdglTypes.IImageWrapper curimage => this.image;

            public int imageflag => this.BufferStruct.imageflag;

            public double width => this.BufferStruct.width;

            public int activeid => this.BufferStruct.activeid;

            public int enableactiveid => this.BufferStruct.enableactiveid;

            public int containscolors => this.BufferStruct.containscolors;

            public vdglTypes.Colorflag colorflag => this.BufferStruct.colorflag;

            public vdglTypes.drawBufferFlag drawFlag => this.BufferStruct.drawFlag;

            public int textureon => this.BufferStruct.textureon;

            public int lighting => this.BufferStruct.lighting;

            public int depthon => this.BufferStruct.depthon;

            public int depthWriteOn => this.BufferStruct.depthWriteOn;

            public int highlighton => this.BufferStruct.highlighton;

            public int polygonmode => this.BufferStruct.polygonmode;

            public int coloron => this.BufferStruct.coloron;

            public int transparentorder => this.BufferStruct.transparentorder;

            public unsafe double this[int index] => this.buf[index];

            public void Free()
            {
            }
        }

        private delegate void SetProjectionMatrix_dll_delegate(
          IntPtr contextPtr,
          double m0,
          double m1,
          double m2,
          double m3,
          double m4,
          double m5,
          double m6,
          double m7,
          double m8,
          double m9,
          double m10,
          double m11,
          double m12,
          double m13,
          double m14,
          double m15);

        private delegate void SetModelMatrix_dll_delegate(
          IntPtr contextPtr,
          double m0,
          double m1,
          double m2,
          double m3,
          double m4,
          double m5,
          double m6,
          double m7,
          double m8,
          double m9,
          double m10,
          double m11,
          double m12,
          double m13,
          double m14,
          double m15);

        [ComVisible(false)]
        internal struct _STATUS
        {
            public IntPtr AllocBytes;
        }

        private delegate void Status_delegate(IntPtr contextPtr, ref vdglDLL._STATUS status);
    }
}
