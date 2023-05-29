using System;
using VectorDraw.Geometry;

namespace VectorDraw.Render
{
    public static class vdgl
    {
        private static vdgl.dumb _dumb = new vdgl.dumb();
        public static long nLists = 0;
        public static long nTextures = 0;
        public static vdglTypes.WrapImage_delegate WrapImage;
        public static vdglTypes.WrapBuffer_delegate WrapBuffer;
        public static vdglTypes.SetProjectionMatrix_delegate SetProjectionMatrix;
        public static vdglTypes.SetModelMatrix_delegate SetModelMatrix;
        public static vdglTypes.SetViewport_delegate SetViewport;
        public static vdglTypes.MarkListForUpdate_delegate MarkListForUpdate;
        public static vdglTypes.IsListNeedUpdate_delegate IsListNeedUpdate;
        public static vdglTypes.GetMemoryAllocs_delegate GetMemoryAllocs;
        public static vdglTypes.FlushDrawBuffers_delegate FlushDrawBuffers;
        public static vdglTypes.GetListStatus_delegate GetListStatus;
        public static vdglTypes.SetFunctionOverride_delegate SetFunctionOverride;
        public static vdglTypes.PushObjectId_delegate PushObjectId;
        public static vdglTypes.PopObjectId_delegate PopObjectId;
        public static vdglTypes.CreateContext_delegate CreateContext;
        public static vdglTypes.DeleteContext_delegate DeleteContext;
        public static vdglTypes.SetBitmapContext_delegate SetBitmapContext;
        public static vdglTypes.Finish_delegate Finish;
        public static vdglTypes.ApplyFilter_delegate ApplyFilter;
        public static vdglTypes.GetPropertyValue_delegate GetPropertyValue;
        public static vdglTypes.SetPropertyValue_delegate SetPropertyValue;
        public static vdglTypes.ClearContext_delegate ClearContext;
        public static vdglTypes.BindPattern_delegate BindPattern;
        public static vdglTypes.AddPatternLine_delegate AddPatternLine;
        public static vdglTypes.DeletePattern_delegate DeletePattern;
        public static vdglTypes.BindLineType_delegate BindLineType;
        public static vdglTypes.AddLineTypeSegment_delegate AddLineTypeSegment;
        public static vdglTypes.DeleteLineType_delegate DeleteLineType;
        public static vdglTypes.SetMaterial_delegate SetMaterial;
        public static vdglTypes.SetFadeEffect_delegate SetFadeEffect;
        public static vdglTypes.SetLightProps_delegate SetLightProps;
        public static vdglTypes.SetSectionProps_delegate SetSectionProps;
        public static vdglTypes.Element_String_Init_delegate Element_String_Init;
        public static vdglTypes.Element_String_AddChar_delegate Element_String_AddChar;
        public static vdglTypes.Element_String_Draw_delegate Element_String_Draw;
        public static vdglTypes.AddOpenGLListId_delegate AddOpenGLListId;
        public static vdglTypes.PFaceDraw_delegate PFaceDraw;
        public static vdglTypes.ElementInit_delegate ElementInit;
        public static vdglTypes.ElementSetPattern_delegate ElementSetPattern;
        public static vdglTypes.ElementDraw_delegate ElementDraw;
        public static vdglTypes.ElementDrawMesh_delegate ElementDrawMesh;
        public static vdglTypes.ElementSetNormal_delegate ElementSetNormal;
        public static vdglTypes.ElementSetVertex_delegate ElementSetVertex;
        public static vdglTypes.ElementSetTexture_delegate ElementSetTexture;
        public static vdglTypes.ElementSetColor_delegate ElementSetColor;
        public static vdglTypes.CreateList_delegate CreateList;
        public static vdglTypes.StartNewList_delegate StartNewList;
        public static vdglTypes.FinishList_delegate FinishList;
        public static vdglTypes.EmptyList_delegate EmptyList;
        public static vdglTypes.IsEmptyList_delegate IsEmptyList;
        public static vdglTypes.SetListFlag_delegate SetListFlag;
        public static vdglTypes.GetListFlag_delegate GetListFlag;
        public static vdglTypes.DrawList_delegate DrawList;
        public static vdglTypes.ReadBuffer_delegate ReadBuffer;
        public static vdglTypes.GetIdAtPixel_delegate GetIdAtPixel;
        public static vdglTypes.GetDepthAtPixel_delegate GetDepthAtPixel;
        public static vdglTypes.SetPickMode_delegate SetPickMode;
        public static vdglTypes.ContainsPoint_delegate ContainsPoint;
        public static vdglTypes.SetClipBox_delegate SetClipBox;
        public static vdglTypes.QuickDraw2dPixelLine_delegate QuickDraw2dPixelLine;
        public static vdglTypes.DrawPixelCross_delegate DrawPixelCross;
        public static vdglTypes.QuickDraw2dPixelBox_delegate QuickDraw2dPixelBox;
        public static vdglTypes.DrawPixelLine_delegate DrawPixelLine;
        public static vdglTypes.DrawPixelBox_delegate DrawPixelBox;
        public static vdglTypes.TestClipBox_delegate TestClipBox;
        public static vdglTypes.ApplyGradient_delegate ApplyGradient;
        public static vdglTypes.PushClipPolygon_delegate PushClipPolygon;
        public static vdglTypes.PopClipPolygon_delegate PopClipPolygon;
        public static vdglTypes.PushAlignToView_delegate PushAlignToView;
        public static vdglTypes.PopAlignToView_delegate PopAlignToView;
        public static vdglTypes.DisableClipId_delegate DisableClipId;
        public static vdglTypes.SetActiveId_delegate SetActiveId;
        public static vdglTypes.PolygonModeOverWrite_delegate PolygonModeOverWrite;
        public static vdglTypes.LockGL_delegate LockGL;
        public static vdglTypes.UnLockGL_delegate UnLockGL;
        public static vdglTypes.CreateSectionCoverFacesList_delegate CreateSectionCoverFacesList;
        public static vdglTypes.DeleteList_delegate DeleteList;
        public static vdglTypes.BindImage_delegate BindImage;
        public static vdglTypes.DeleteImage_delegate DeleteImage;

        public static vdgl.RenderEngine RenderType => vdgl._dumb.mRenderType;

        public static Matrix ReadMatrix(double[] dm)
        {
            Matrix matrix = new Matrix();
            try {
                matrix.A00 = (dm[0]);
                matrix.A01 = (dm[1]);
                matrix.A02 = (dm[2]);
                matrix.A03 = (dm[3]);
                matrix.A10 = (dm[4]);
                matrix.A11 = (dm[5]);
                matrix.A12 = (dm[6]);
                matrix.A13 = (dm[7]);
                matrix.A20 = (dm[8]);
                matrix.A21 = (dm[9]);
                matrix.A22 = (dm[10]);
                matrix.A23 = (dm[11]);
                matrix.A30 = (dm[12]);
                matrix.A31 = (dm[13]);
                matrix.A32 = (dm[14]);
                matrix.A33 = (dm[15]);
 
            }
            catch { }
            finally
            {

            }
            return matrix;
        }

        public static double[] AsvdrawContextMatrix(Matrix mat)
        {
            if (mat == (Matrix)null)
                return (double[])null;
            return new double[16]
            {
        mat.A00,
        mat.A10,
        mat.A20,
        mat.A30,
        mat.A01,
        mat.A11,
        mat.A21,
        mat.A31,
        mat.A02,
        mat.A12,
        mat.A22,
        mat.A32,
        mat.A03,
        mat.A13,
        mat.A23,
        mat.A33
            };
        }

        public static vdglTypes.MemoryStatus GetMemoryStatus() => new vdglTypes.MemoryStatus()
        {
            AllocBytes = vdgl.GetMemoryAllocs(),
            GLNumLists = vdgl.nLists,
            GLNumTextures = vdgl.nTextures
        };

        private class dumb
        {
            internal vdgl.RenderEngine mRenderType;

            public dumb()
            {
                try
                {
                    vdglDLL.SetDefault();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public enum RenderEngine
        {
            Unmanage,
            Manage,
        }
    }
}
