using System;
using System.Runtime.InteropServices;
using VectorDraw.Geometry;
using VectorDraw.Serialize;

namespace VectorDraw.Render
{
    public static class vdglTypes
    {
        public delegate void DrawElement_StringDelegate(
          IntPtr chars,
          int nchars,
          int flag,
          IntPtr FontName,
          int FontNameLength,
          float FontSize,
          int FontStyle,
          [MarshalAs(UnmanagedType.LPArray, SizeConst = 4)] byte[] colorRGBA,
          float thickness,
          [MarshalAs(UnmanagedType.LPArray, SizeConst = 4)] float[] box2d,
          [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)] double[] modelmatrix,
          [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)] double[] world2pixel,
          ref int Cancel);

        public delegate void DrawElementDelegate(IntPtr ElementPtr, ref int Cancel);

        public delegate void DrawElementSuccedDelegate(
          [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)] double[] modelmatrix,
          double minz,
          double DistanceFromCenter,
          int elementUserId,
          int vertexUserId,
          vdglTypes.SelectStatusCode statusCode,
          int isFill,
          ref int Cancel);

        public delegate void PushAlignToViewDelegate(
          byte Flag,
          [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)] double[] InsertionPoint,
          [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)] double[] ExtrusionVector,
          [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)] double[] modelmatrix,
          int AlignToViewSize,
          double objectHeight,
          double objectRotation);

        public delegate void PopAlignToViewDelegate();

        public delegate void ImageBindDelegate(IntPtr image, vdglTypes.MATERIAL_FLAG materialFlag);

        public delegate void ImageBindCreatedDelegate(IntPtr image, int Flag);

        public delegate void PFNONDRAWARRAYSPROC(IntPtr vdcontext, IntPtr drawbuffer);

        public delegate void PFNONMESHPROC(
          IntPtr contextPtr,
          int mesh_items,
          byte mesh_stride,
          [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)] double[] midpoint,
          IntPtr mesh_verts,
          IntPtr mesh_normals,
          IntPtr colors,
          IntPtr textures,
          IntPtr edges);

        public interface IBufferWrapper
        {
            int type { get; }

            object context { get; }

            int curitem { get; }

            double this[int index] { get; }

            vdglTypes.IImageWrapper curimage { get; }

            int imageflag { get; }

            double width { get; }

            int activeid { get; }

            int enableactiveid { get; }

            int containscolors { get; }

            vdglTypes.Colorflag colorflag { get; }

            vdglTypes.drawBufferFlag drawFlag { get; }

            int textureon { get; }

            int lighting { get; }

            int depthon { get; }

            int depthWriteOn { get; }

            int highlighton { get; }

            int polygonmode { get; }

            int coloron { get; }

            int transparentorder { get; }

            void Free();
        }

        public interface IImageWrapper
        {
            short bindType { get; }

            byte InterpolationMethod { get; }

            int width { get; }

            int height { get; }

            int stridewidth { get; }

            bool IsNull { get; }

            void CopyTo(IntPtr dst);

            IntPtr ToIntPtr { get; }
        }

        [ComVisible(false)]
        public struct MemoryStatus
        {
            public long AllocBytes;
            public long GLNumLists;
            public long GLNumTextures;
        }

        [Flags]
        public enum LIST_FLAG
        {
            None = 0,
            MarkAsUpdate = 64, // 0x00000040
        }

        public enum SelectStatusCode
        {
            CompletelyInside,
            Noclip,
            ClipSomeHow,
        }

        [Flags]
        public enum drawBufferFlag
        {
            None = 0,
            ForceEnableTexture = 1,
            ForceDisableTexture = 2,
            ForceDisableLight = 4,
            IsFillOn = 8,
            ForceEnableFill = 32, // 0x00000020
            ForceDisableColorMaskOnHide = 64, // 0x00000040
        }

        public enum Colorflag
        {
            Truecolor,
            Forground,
            Background,
            ByBlock,
        }

        public enum PickMode
        {
            NONE,
            CROSS_W,
            WINDOW,
            CROSS_P,
            WINDOW_P,
            FENCE,
        }

        public enum PropertyType
        {
            POLYGONMODE = 1,
            ENABLE_TEXTURE = 2,
            ENABLE_LIGHT = 3,
            ENABLE_ZBUFFER = 4,
            ENABLE_COLORBUFFER = 5,
            FILLON_LINEWIDTH = 6,
            AMBIENT = 7,
            DEPTH_OFFSET = 8,
            DEPTH_RANGE = 9,
            HIGHLIGHT = 10, // 0x0000000A
            MATERIAL_LOCK = 11, // 0x0000000B
            TRANSPARENT_ORDER = 12, // 0x0000000C
            FORGROUND = 13, // 0x0000000D
            BACKGROUND = 14, // 0x0000000E
            SHAPETHICKNESS = 15, // 0x0000000F
            DRAWN_BOX = 16, // 0x00000010
            DISABLESECTIONS = 17, // 0x00000011
            POLYGON_STIPPLE = 18, // 0x00000012
            LINE_STIPPLE = 19, // 0x00000013
            DRAWEDGE_MODE = 20, // 0x00000014
            SHOWHIDENEDGES = 21, // 0x00000015
            RENDERINGHIGHTQUALITY = 22, // 0x00000016
            EDGECOLOR = 23, // 0x00000017
            PENCAPSSQUARE = 24, // 0x00000018
            HIDE_SOLID_REGIONS = 25, // 0x00000019
            LIGHT_FILTER = 26, // 0x0000001A
            COLOR_MIX = 27, // 0x0000001B
            ACTION_RENDER_PROPS = 28, // 0x0000001C
            EXTRALINETYPESCALE = 29, // 0x0000001D
            ENABLE_BUFFER_ID = 30, // 0x0000001E
            ENABLE_ZBUFFER_WRITE = 31, // 0x0000001F
            ALPHAFUNC = 32, // 0x00000020
            IS_GL_LIST = 33, // 0x00000021
            MIN_PEN_WIDTH = 34, // 0x00000022
            IGNORE_TRANSPARENCY = 35, // 0x00000023
            TOTALCOUNTOURSEGMENTS = 36, // 0x00000024
            TOTALCLIPCOUNTOURS = 37, // 0x00000025
            SHXBOLDWIDTH = 38, // 0x00000026
            PARALLELFILL = 39, // 0x00000027
            DPIPATTERNOFFSET = 40, // 0x00000028
            OPENGLFLAGS = 41, // 0x00000029
            LINEWEIGHTOFF = 42, // 0x0000002A
            HAS_TRANSPARENT = 100, // 0x00000064
        }

        public enum PropertyTypeGet
        {
            POLYGONMODE = 1,
            ENABLE_TEXTURE = 2,
            ENABLE_LIGHT = 3,
            ENABLE_ZBUFFER = 4,
            ENABLE_COLORBUFFER = 5,
            HIGHLIGHT = 10, // 0x0000000A
            TRANSPARENT_ORDER = 12, // 0x0000000C
            DRAWEDGE_MODE = 20, // 0x00000014
            HIDE_SOLID_REGIONS = 25, // 0x00000019
            LIGHT_FILTER = 26, // 0x0000001A
            COLOR_MIX = 27, // 0x0000001B
            ENABLE_BUFFER_ID = 30, // 0x0000001E
            ENABLE_ZBUFFER_WRITE = 31, // 0x0000001F
            HAS_TRANSPARENT = 100, // 0x00000064
            ACTIVECOLOR = 101, // 0x00000065
            CLEARCOLOR = 102, // 0x00000066
        }

        public enum ALPHAFUNC_ID
        {
            ALWAYS,
            GEQUAL,
            NOTEQUAL,
            LESS,
            EQUAL,
            NEVER,
        }

        public enum PropertyValues
        {
            COLORBUFFER_OFF = 0,
            DRAWEDGE_MODE_DEFAULT = 0,
            HIGHLIGHT_OFF = 0,
            LIGHT_OFF = 0,
            MATERIAL_LOCK_OFF = 0,
            TEXTURE_OFF = 0,
            TRANSPARENT_ORDER_NONE = 0,
            ZBUFFER_OFF = 0,
            COLORBUFFER_ON = 1,
            DRAWEDGE_MODE_HIDE = 1,
            HIGHLIGHT_ON = 1,
            LIGHT_ON = 1,
            MATERIAL_LOCK_ON = 1,
            POLYGON_MODE_LINES = 1,
            TEXTURE_ON = 1,
            TRANSPARENT_ORDER_OFF = 1,
            ZBUFFER_ON = 1,
            DRAWEDGE_MODE_SHADE_ON = 2,
            POLYGON_MODE_FILL = 2,
            TRANSPARENT_ORDER_ON = 2,
            TRANSPARENT_ORDER_ON_2 = 3,
        }

        public enum COLOR_MIX
        {
            None,
            Visible,
        }

        [Flags]
        public enum FLAG_CLEAR
        {
            NONE = 0,
            BACKGROUND = 1,
            DEPTH_BUFFER = 2,
            GRADIENT_COLOR = 4,
        }

        public enum PenWidthFlag
        {
            DMM,
            DU,
            PIXEL,
        }

        [Flags]
        public enum FLAG_ELEMENT
        {
            None = 0,
            USE_TEXTURE = 1,
            CLOSE_FIGURE = 2,
            FILL_ON = 4,
            IMAGE = 8,
            FILL_ALWAYS = 16, // 0x00000010
            USE_COLORS = 32, // 0x00000020
            USE_NORMALS = 64, // 0x00000040
            USE_GRADIENT = 128, // 0x00000080
            CONTAINS_USERID = 256, // 0x00000100
            CONTAINS_FLAGS = 512, // 0x00000200
            COMPLEX_POLY = 1024, // 0x00000400
            FONT_SHAPE = 2048, // 0x00000800
            MAPPED_IMAGE = 4096, // 0x00001000
            BOUND_FILL = 8192, // 0x00002000
            FROM_TRIANGL_STRIP = 16384, // 0x00004000
            EXPORT_FLAG_ELEMENT_IS_FOREGROUND = 32768, // 0x00008000
            IGNORE_LIGHTING = 65536, // 0x00010000
            COMPLEX_POLY_GL_SOLID_HATCH = 131072, // 0x00020000
            TEXT_BOUND_FILL = 262144, // 0x00040000
            SECTION_FILL = 524288, // 0x00080000
            SECTION_LINES = 1048576, // 0x00100000
            FONT_SHAPE_THICK = 2097152, // 0x00200000
            HIDE_OFF = 4194304, // 0x00400000
            PENCAPSSQUARE = 8388608, // 0x00800000
            HATCHVIEW = 67108864, // 0x04000000
            HATCHLINEAR = 134217728, // 0x08000000
            THICK_SEGMENT = 268435456, // 0x10000000
        }

        [Flags]
        public enum FLAG_VERTEX
        {
            None = 0,
            INVISIBLE = 2,
            END_POLY = 4,
        }

        public enum FunctionType
        {
            None = 0,
            DrawElement = 2,
            DrawElementSucced = 4,
            ENUM_IMAGE_BIND = 128, // 0x00000080
            DrawElement_String = 256, // 0x00000100
            PUSHALIGNTOVIEW = 1024, // 0x00000400
            POPALIGNTOVIEW = 2048, // 0x00000800
            ENUM_IMAGE_BIND_CREATED = 32768, // 0x00008000
            DRAWARRAYS = 65537, // 0x00010001
            DRAWMESH = 65538, // 0x00010002
        }

        [Flags]
        public enum MATERIAL_FLAG
        {
            NONE = 0,
            BYBLOCKCOLOR = 1,
            BYBLOCKCOLOR_ALPHA = 2,
            BYBLOCKLINEWEIGHT = 4,
            BYBLOCKLINETYPE = 8,
            BYBLOCK_ALL = BYBLOCKLINETYPE | BYBLOCKLINEWEIGHT | BYBLOCKCOLOR_ALPHA | BYBLOCKCOLOR, // 0x0000000F
            COLORISFORGROUND = 16, // 0x00000010
            MAPPEDIMAGE = 32, // 0x00000020
            COLORISBACKGROUND = 64, // 0x00000040
            PUSHED = 128, // 0x00000080
            POPED = 256, // 0x00000100
            IGNORE_LOCK = 512, // 0x00000200
            COLORISTRUECOLOR = 1024, // 0x00000400
        }

        [Flags]
        public enum ScanFlag
        {
            NONE = 0,
            ALLEDGES = 1,
            OSNAPS = 2,
            TOOLTIP = 4,
        }

        [Flags]
        public enum ListStatus
        {
            NONE = 0,
            STRING = 1,
            DPI = 2,
            ALIGNTOVIEW = 4,
            GLLIST = 8,
            SUBLIST = 16, // 0x00000010
            ALIGNTOVIEW__STRECHTEXT = 32, // 0x00000020
            CONTAINS_UPDATE = 64, // 0x00000040
        }

        public enum lockStatus
        {
            None,
            OPENGL,
            VDRAW_VBO,
        }

        public delegate vdglTypes.IImageWrapper WrapImage_delegate(IntPtr image);

        public delegate vdglTypes.IBufferWrapper WrapBuffer_delegate(IntPtr buffer);

        public delegate void SetProjectionMatrix_delegate(IntPtr vdrawGlContext, Matrix m);

        public delegate void SetModelMatrix_delegate(IntPtr vdrawGlContext, Matrix m);

        public delegate void SetViewport_delegate(
          IntPtr vdrawGlContext,
          int uplx,
          int uply,
          int w,
          int h);

        public delegate Matrix ReadMatrix_delegate(double[] dm);

        public delegate void MarkListForUpdate_delegate(IntPtr list);

        public delegate bool IsListNeedUpdate_delegate(IntPtr list);

        public delegate long GetMemoryAllocs_delegate();

        public delegate void FlushDrawBuffers_delegate(IntPtr contextPtr, int mode);

        public delegate vdglTypes.ListStatus GetListStatus_delegate(
          IntPtr contextPtr,
          IntPtr list);

        public delegate void SetFunctionOverride_delegate(
          IntPtr contextPtr,
          vdglTypes.FunctionType functionType,
          IntPtr funcPtr);

        public delegate void PushObjectId_delegate(IntPtr contextPtr, int id);

        public delegate void PopObjectId_delegate(IntPtr contextPtr);

        public delegate IntPtr CreateContext_delegate();

        public delegate void DeleteContext_delegate(ref IntPtr contextPtr);

        public delegate void SetBitmapContext_delegate(
          IntPtr contextPtr,
          int width,
          int height,
          IntPtr bytes,
          int[] clipViewPort,
          double dpi);

        public delegate int Finish_delegate(
          IntPtr contextPtr,
          ref IntPtr srcbytes,
          ref int length,
          int[] DrawnBox);

        public delegate void ApplyFilter_delegate(
          IntPtr context,
          IntPtr data,
          int width,
          int height,
          int type);

        public delegate void GetPropertyValue_delegate(
          IntPtr contextPtr,
          vdglTypes.PropertyTypeGet PropertyType,
          ref double PropertyValue);

        public delegate void SetPropertyValue_delegate(
          IntPtr contextPtr,
          vdglTypes.PropertyType PropertyType,
          double[] PropertyValue);

        public delegate void ClearContext_delegate(
          IntPtr contextPtr,
          int r,
          int g,
          int b,
          int a,
          int gradientR,
          int gradientG,
          int gradientB,
          int GradientAngle,
          vdglTypes.FLAG_CLEAR Flag,
          IntPtr srcbytes,
          int srcwidth,
          int srcheight,
          int srcoffsetX,
          int srcoffsetY);

        public delegate IntPtr BindPattern_delegate(IntPtr contextPtr, IntPtr bindId, byte IsDPI);

        public delegate bool AddPatternLine_delegate(
          IntPtr contextPtr,
          IntPtr bindId,
          double angle,
          double originx,
          double originy,
          double offsetx,
          double offsety,
          uint ndashes,
          double[] dashes);

        public delegate void DeletePattern_delegate(IntPtr pattern);

        public delegate IntPtr BindLineType_delegate(
          IntPtr contextPtr,
          IntPtr bindId,
          byte IsDPI,
          byte drawMethod);

        public delegate bool AddLineTypeSegment_delegate(
          IntPtr contextPtr,
          IntPtr bindId,
          double dash,
          IntPtr shape);

        public delegate void DeleteLineType_delegate(IntPtr linetype);

        public delegate void SetMaterial_delegate(
          IntPtr contextPtr,
          int r,
          int g,
          int b,
          int a,
          vdglTypes.PenWidthFlag penWidthType,
          double penWidth,
          IntPtr linetypeId,
          double linetypeScale,
          IntPtr ImageId,
          double[] imageMatrix,
          int gradientType,
          int gRed,
          int gGreen,
          int gBlue,
          vdglTypes.MATERIAL_FLAG materialFlag);

        public delegate void SetFadeEffect_delegate(IntPtr contextPtr, int fade);

        public delegate void SetLightProps_delegate(
          IntPtr contextPtr,
          int lightId,
          int Enable,
          int TypeOfLight,
          double Intensity,
          double PositionX,
          double PositionY,
          double PositionZ,
          double DirectionX,
          double DirectionY,
          double DirectionZ,
          int cR,
          int cG,
          int cB,
          double SpotFallOff,
          double SpotAngle);

        public delegate void SetSectionProps_delegate(
          IntPtr contextPtr,
          int sectionId,
          int dictId,
          int Enable,
          double OriginX,
          double OriginY,
          double OriginZ,
          double DirectionX,
          double DirectionY,
          double DirectionZ);

        public delegate void Element_String_Init_delegate(
          IntPtr contextPtr,
          int flag,
          IntPtr FontName,
          int FontNamelength,
          float FontSize,
          int FontStyle,
          float thickness,
          float xmin,
          float ymin,
          float xmax,
          float ymax);

        public delegate void Element_String_AddChar_delegate(
          IntPtr contextPtr,
          ushort Char,
          IntPtr list,
          float offsetX,
          float offsetY);

        public delegate void Element_String_Draw_delegate(IntPtr contextPtr);

        public delegate void AddOpenGLListId_delegate(
          IntPtr contextPtr,
          uint listid,
          uint transparency);

        public delegate bool PFaceDraw_delegate(
          IntPtr contextPtr,
          bool clockwise,
          gPoints VertexList,
          Int32Array FaceList,
          DoubleArray textcoords,
          double smoothangle,
          IElevatedColors ecolors,
          IgrSystemColorPalette palette);

        public delegate IntPtr ElementInit_delegate(
          IntPtr contextPtr,
          vdglTypes.FLAG_ELEMENT Flag,
          int UserId);

        public delegate void ElementSetPattern_delegate(
          IntPtr contextPtr,
          IntPtr ElementPtr,
          IntPtr patternId,
          double originx,
          double originy,
          double originz);

        public delegate void ElementDraw_delegate(IntPtr contextPtr, IntPtr ElementPtr);

        public delegate void ElementDrawMesh_delegate(
          IntPtr contextPtr,
          int mesh_items,
          byte mesh_stride,
          [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)] double[] midpoint,
          IntPtr mesh_verts,
          IntPtr mesh_normals,
          IntPtr colors,
          IntPtr textures,
          IntPtr edges);

        public delegate void ElementSetNormal_delegate(
          IntPtr contextPtr,
          IntPtr ElementPtr,
          double x,
          double y,
          double z);

        public delegate void ElementSetVertex_delegate(
          IntPtr contextPtr,
          IntPtr ElementPtr,
          double x,
          double y,
          double z,
          vdglTypes.FLAG_VERTEX Flag,
          int UserId);

        public delegate void ElementSetTexture_delegate(
          IntPtr contextPtr,
          IntPtr ElementPtr,
          double u,
          double v,
          double w);

        public delegate void ElementSetColor_delegate(
          IntPtr contextPtr,
          IntPtr ElementPtr,
          byte r,
          byte g,
          byte b,
          byte a);

        public delegate IntPtr CreateList_delegate(IntPtr contextPtr);

        public delegate void StartNewList_delegate(IntPtr contextPtr, IntPtr list);

        public delegate void FinishList_delegate(IntPtr contextPtr);

        public delegate void EmptyList_delegate(IntPtr list);

        public delegate bool IsEmptyList_delegate(IntPtr list);

        public delegate void SetListFlag_delegate(IntPtr list, byte Flag);

        public delegate int GetListFlag_delegate(IntPtr list);

        public delegate int DrawList_delegate(IntPtr contextPtr, IntPtr list);

        public delegate int ReadBuffer_delegate(IntPtr contextPtr, int buffertype, IntPtr buffer);

        public delegate uint GetIdAtPixel_delegate(IntPtr contextPtr, int x, int y);

        public delegate double GetDepthAtPixel_delegate(IntPtr contextPtr, int x, int y);

        public delegate void SetPickMode_delegate(
          IntPtr context,
          vdglTypes.PickMode pickmode,
          double[] coords,
          int npoints,
          vdglTypes.ScanFlag scanFlag);

        public delegate bool ContainsPoint_delegate(IntPtr context, int px, int py);

        public delegate void SetClipBox_delegate(IntPtr context, int x, int y, int width, int height);

        public delegate void QuickDraw2dPixelLine_delegate(
          IntPtr context,
          int sx,
          int sy,
          int ex,
          int ey,
          byte r,
          byte g,
          byte b,
          byte a);

        public delegate vdglTypes.SelectStatusCode DrawPixelCross_delegate(
          IntPtr context,
          int sx,
          int sy,
          double sz,
          int size,
          byte r,
          byte g,
          byte b,
          byte a,
          int penwidth);

        public delegate void QuickDraw2dPixelBox_delegate(IntPtr context, int cx, int cy, int size);

        public delegate void DrawPixelLine_delegate(
          IntPtr context,
          int sx,
          int sy,
          double sz,
          int ex,
          int ey,
          double ez);

        public delegate void DrawPixelBox_delegate(
          IntPtr context,
          int left,
          int top,
          int width,
          int height,
          int flag);

        public delegate int TestClipBox_delegate(
          IntPtr context,
          double xmin,
          double ymin,
          double zmin,
          double xmax,
          double ymax,
          double zmax);

        public delegate void ApplyGradient_delegate(
          IntPtr bytes,
          int width,
          int height,
          int clipleft,
          int cliptop,
          int clipright,
          int clipbottom,
          int r,
          int g,
          int b,
          int a,
          int gradientR,
          int gradientG,
          int gradientB,
          int GradientAngle);

        public delegate int PushClipPolygon_delegate(IntPtr context, double[] pts, int npts);

        public delegate void PopClipPolygon_delegate(IntPtr context);

        public delegate void PushAlignToView_delegate(
          IntPtr context,
          byte Flag,
          double InsertionPointX,
          double InsertionPointY,
          double InsertionPointZ,
          double ExtrusionVectorX,
          double ExtrusionVectorY,
          double ExtrusionVectorZ,
          int AlignToViewSize,
          double objectHeight,
          double objectRotation);

        public delegate void PopAlignToView_delegate(IntPtr context);

        public delegate void DisableClipId_delegate(IntPtr context, int clipId, int bDisable);

        public delegate void SetActiveId_delegate(IntPtr context, uint Id);

        public delegate void PolygonModeOverWrite_delegate(IntPtr context, int mode);

        public delegate vdglTypes.lockStatus LockGL_delegate(
          IntPtr context,
          vdglTypes.lockStatus lstat);

        public delegate void UnLockGL_delegate(IntPtr context);

        public delegate IntPtr CreateSectionCoverFacesList_delegate(
          IntPtr contextPtr,
          IntPtr newList,
          double sectionOriginX,
          double sectionOriginY,
          double sectionOriginZ,
          double sectionDirX,
          double sectionDirY,
          double sectionDirZ,
          IntPtr enumlist,
          int red,
          int green,
          int blue,
          int alpha,
          int flag);

        public delegate void DeleteList_delegate(IntPtr list);

        public delegate IntPtr BindImage_delegate(
          IntPtr contextPtr,
          IntPtr bindId,
          IntPtr bytes,
          int size,
          int width,
          int height,
          int interpolationMode);

        public delegate void DeleteImage_delegate(IntPtr image);
    }
}
