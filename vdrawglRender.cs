using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using VectorDraw.Actions;
using VectorDraw.DrawElements;
using VectorDraw.Generics;
using VectorDraw.Geometry;
using VectorDraw.Geometry.GpcWrapper;
using VectorDraw.Render.GDIDraw;
using VectorDraw.Render.OpenGL;
using VectorDraw.Serialize;
using VectorDraw.WinMessages;

namespace VectorDraw.Render
{
    /// <summary>VectorDraw render class.</summary>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IvdRender))]
    [Guid("FDF56B0A-D854-4d10-A2DF-4F8A3A32B4F7")]
    public class vdrawglRender : WrapperRender, IRenderList
    {
        internal bool SupportWire3d_Transparent = true;
        internal const double GLOBALAMBIENT = 0.15;
        private LightManager mLManager = new LightManager();
        internal bool mDestoyed;
        private IntPtr vdrawGlContext = IntPtr.Zero;
        private Stack<VectorDraw.Geometry.Matrix> sectionApplyModelMatrix = new Stack<VectorDraw.Geometry.Matrix>();
        private bool mPenStyleNeedUpdate = true;
        internal bool mPenStyleDisableWrite;
        internal int mListDepth;
        internal BitmapData bmpData;
        private bool mIsBlenOn;
        private bool mIsDrawEdgeOn;
        internal bool mForceBlending;
        private Stack<bool> DrawWireStack = new Stack<bool>();
        private vdglTypes.DrawElement_StringDelegate DrawElement_StringFunc;
        private vdglTypes.DrawElementDelegate DrawElementFunc;
        private vdglTypes.DrawElementSuccedDelegate DrawElementSuccedFunc;
        private vdglTypes.ImageBindDelegate ImageBindFunc;
        private vdglTypes.ImageBindCreatedDelegate ImageBindCreatedFunc;
        private vdglTypes.PushAlignToViewDelegate PushAlignToViewFunc;
        private vdglTypes.PopAlignToViewDelegate PopAlignToViewFunc;
        private vdglTypes.PFNONDRAWARRAYSPROC DrawArraysFunc;
        private vdglTypes.PFNONMESHPROC DrawMeshFunc;
        internal bool mIsQuickLock;
        private double[] polygonStipple = new double[64]
        {
      1.0,
      0.0,
      1.0,
      0.0,
      1.0,
      0.0,
      1.0,
      0.0,
      0.0,
      1.0,
      0.0,
      1.0,
      0.0,
      1.0,
      0.0,
      1.0,
      1.0,
      0.0,
      1.0,
      0.0,
      1.0,
      0.0,
      1.0,
      0.0,
      0.0,
      1.0,
      0.0,
      1.0,
      0.0,
      1.0,
      0.0,
      1.0,
      1.0,
      0.0,
      1.0,
      0.0,
      1.0,
      0.0,
      1.0,
      0.0,
      0.0,
      1.0,
      0.0,
      1.0,
      0.0,
      1.0,
      0.0,
      1.0,
      1.0,
      0.0,
      1.0,
      0.0,
      1.0,
      0.0,
      1.0,
      0.0,
      0.0,
      1.0,
      0.0,
      1.0,
      0.0,
      1.0,
      0.0,
      1.0
        };
        private double[] lineStipple = new double[16]
        {
      0.0,
      0.0,
      0.0,
      0.0,
      1.0,
      1.0,
      1.0,
      1.0,
      0.0,
      0.0,
      0.0,
      0.0,
      1.0,
      1.0,
      1.0,
      1.0
        };
        private bool mDrawStipple;
        internal Vector DrawPolygon_Vector;

        /// <summary>
        /// Returns the <see cref="T:VectorDraw.Render.OpenGL.LightManager" /> used for apply lights and shadows
        /// </summary>
        public virtual LightManager LManager => this.mLManager;

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.Update" />
        /// </summary>
        public override void Update()
        {
            base.Update();
            this.LManager.Update((IRenderingLight)null);
        }

        internal static void TransformUV(VectorDraw.Geometry.Matrix _w2ecs, VectorDraw.Geometry.Matrix tmat, gPoint p0, ref gPoint uv0)
        {
            if (_w2ecs != (VectorDraw.Geometry.Matrix)null)
                _w2ecs.TransformPt(p0, uv0);
            else
                uv0.CopyFrom(p0);
            bool flag = !Globals.AreEqual(tmat.A22, 1.0, Globals.Default3DMatrixEquality) || !Globals.AreEqual(tmat.A30, 0.0, Globals.Default3DMatrixEquality) && !Globals.AreEqual(tmat.A31, 0.0, Globals.Default3DMatrixEquality) && !Globals.AreEqual(tmat.A33, 1.0, Globals.Default3DMatrixEquality);
            if (!flag)
                uv0.z = 0.0;
            tmat.TransformRefPt(ref uv0);
            if (!flag)
            {
                if (!Globals.AreEqual(uv0.z, 0.0, Globals.DefaultVectorEquality))
                {
                    uv0.x /= uv0.z;
                    uv0.y /= uv0.z;
                }
                uv0.z = 1.0;
            }
            else
            {
                if (!Globals.AreEqual(uv0.z, 0.0, Globals.DefaultVectorEquality))
                    return;
                uv0.z = 1.0;
            }
        }

        internal static bool createEcsToWorldUVS(
          ref gPoint p0,
          ref gPoint p1,
          ref gPoint p2,
          ref gPoint p3,
          ref bool visible1,
          ref bool visible2,
          ref bool visible3,
          ref bool visible4,
          VectorDraw.Geometry.Matrix tmat,
          ref Vector normal,
          ref gPoint uv0,
          ref gPoint uv1,
          ref gPoint uv2,
          ref gPoint uv3)
        {
            VectorDraw.Geometry.Matrix _w2ecs = new VectorDraw.Geometry.Matrix();
            int overlapVertex = 0;
            int orientation = 0;
            if (!vdRender.GetOrientedNormal(p0, p1, p2, p3, ref normal, ref overlapVertex, ref orientation))
                return false;
            gPoint p0_1 = p0;
            gPoint p0_2 = p1;
            gPoint p0_3 = p2;
            gPoint p0_4 = p3;
            bool flag1 = visible1;
            bool flag2 = visible2;
            bool flag3 = visible3;
            bool flag4 = visible4;
            if (vdRenderGlobalProperties.IsFrontFaceClockWise && (double)orientation < 0.0 || !vdRenderGlobalProperties.IsFrontFaceClockWise && (double)orientation > 0.0)
            {
                normal *= -1.0;
                p0_1 = p0;
                p0_2 = p3;
                p0_3 = p2;
                p0_4 = p1;
                if (5 - overlapVertex > 4)
                    ;
                flag1 = visible4;
                flag2 = visible3;
                flag3 = visible2;
                flag4 = visible1;
            }
            p0 = p0_1;
            p1 = p0_2;
            p2 = p0_3;
            p3 = p0_4;
            visible1 = flag1;
            visible2 = flag2;
            visible3 = flag3;
            visible4 = flag4;
            if (tmat != (VectorDraw.Geometry.Matrix)null)
            {
                _w2ecs.IdentityMatrix();
                _w2ecs.ApplyWCS2ECS(normal);
                vdrawglRender.TransformUV(_w2ecs, tmat, p0_1, ref uv0);
                vdrawglRender.TransformUV(_w2ecs, tmat, p0_2, ref uv1);
                vdrawglRender.TransformUV(_w2ecs, tmat, p0_3, ref uv2);
                vdrawglRender.TransformUV(_w2ecs, tmat, p0_4, ref uv3);
            }
            return true;
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.EnableLighting(System.Boolean)" />
        /// </summary>
        public override bool EnableLighting(bool bVal)
        {
            double PropertyValue = 0.0;
            vdgl.GetPropertyValue(this.vdContext, vdglTypes.PropertyTypeGet.ENABLE_LIGHT, ref PropertyValue);
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.ENABLE_LIGHT, new double[1]
            {
        bVal ? 1.0 : 0.0
            });
            return PropertyValue != 0.0;
        }

        internal bool IsEnableLighting
        {
            get
            {
                double PropertyValue = 0.0;
                vdgl.GetPropertyValue(this.vdContext, vdglTypes.PropertyTypeGet.ENABLE_LIGHT, ref PropertyValue);
                return PropertyValue != 0.0;
            }
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.EnableDepthBuffer(System.Boolean)" />
        /// </summary>
        public override bool EnableTexture(bool bvalue)
        {
            double PropertyValue = 0.0;
            vdgl.GetPropertyValue(this.vdContext, vdglTypes.PropertyTypeGet.ENABLE_TEXTURE, ref PropertyValue);
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.ENABLE_TEXTURE, new double[1]
            {
        bvalue ? (double) vdglTypes.PropertyValues.POLYGON_MODE_LINES.GetHashCode() : (double) vdglTypes.PropertyValues.TEXTURE_OFF.GetHashCode()
            });
            return PropertyValue != 0.0;
        }

        /// <summary>
        /// Overrides the <see cref="M:VectorDraw.Render.vdRender.EnableBufferId(System.Boolean)" />
        /// </summary>
        public override bool EnableBufferId(bool bvalue)
        {
            double PropertyValue = 0.0;
            vdgl.GetPropertyValue(this.vdContext, vdglTypes.PropertyTypeGet.ENABLE_BUFFER_ID, ref PropertyValue);
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.ENABLE_BUFFER_ID, new double[1]
            {
        bvalue ? 1.0 : 0.0
            });
            return PropertyValue != 0.0;
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.EnableDepthBuffer(System.Boolean)" />
        /// </summary>
        public override bool EnableDepthBuffer(bool bvalue)
        {
            double PropertyValue = 0.0;
            vdgl.GetPropertyValue(this.vdContext, vdglTypes.PropertyTypeGet.ENABLE_ZBUFFER, ref PropertyValue);
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.ENABLE_ZBUFFER, new double[1]
            {
        bvalue ? (double) vdglTypes.PropertyValues.POLYGON_MODE_LINES.GetHashCode() : (double) vdglTypes.PropertyValues.TEXTURE_OFF.GetHashCode()
            });
            if (this.SupportStringDrawRegistration)
                this.RegisterFunction(vdglTypes.FunctionType.DrawElement_String);
            else
                this.UnRegisterFunction(vdglTypes.FunctionType.DrawElement_String);
            return PropertyValue != 0.0;
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.EnableDepthBufferWrite(System.Boolean)" />
        /// </summary>
        public override bool EnableDepthBufferWrite(bool bvalue)
        {
            double PropertyValue = 0.0;
            vdgl.GetPropertyValue(this.vdContext, vdglTypes.PropertyTypeGet.ENABLE_ZBUFFER_WRITE, ref PropertyValue);
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.ENABLE_ZBUFFER_WRITE, new double[1]
            {
        bvalue ? 1.0 : 0.0
            });
            return PropertyValue != 0.0;
        }

        /// <summary>
        /// overrides the <see cref="P:VectorDraw.Render.vdRender.IsDepthBufferEnable" />
        /// </summary>
        public override bool IsDepthBufferEnable
        {
            get
            {
                double PropertyValue = 0.0;
                vdgl.GetPropertyValue(this.vdContext, vdglTypes.PropertyTypeGet.ENABLE_ZBUFFER, ref PropertyValue);
                return PropertyValue != 0.0;
            }
        }

        /// <summary>
        /// overrides the <see cref="P:VectorDraw.Render.vdRender.IsBufferIdEnable" />
        /// </summary>
        public override bool IsBufferIdEnable
        {
            get
            {
                double PropertyValue = 0.0;
                vdgl.GetPropertyValue(this.vdContext, vdglTypes.PropertyTypeGet.ENABLE_BUFFER_ID, ref PropertyValue);
                return PropertyValue != 0.0;
            }
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.EnableColorBuffer(System.Boolean)" />
        /// </summary>
        public override bool EnableColorBuffer(bool bvalue)
        {
            double PropertyValue = 0.0;
            vdgl.GetPropertyValue(this.vdContext, vdglTypes.PropertyTypeGet.ENABLE_COLORBUFFER, ref PropertyValue);
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.ENABLE_COLORBUFFER, new double[1]
            {
        bvalue ? (double) vdglTypes.PropertyValues.POLYGON_MODE_LINES.GetHashCode() : (double) vdglTypes.PropertyValues.TEXTURE_OFF.GetHashCode()
            });
            return PropertyValue != 0.0;
        }

        /// <summary>
        /// Overrides the <see cref="M:VectorDraw.Render.vdRender.GetIdAtPixel(System.Int32,System.Int32)" />
        /// </summary>
        public override uint GetIdAtPixel(int x, int y) => this.ControlRender != null && this.ControlRender.ActiveRender != null && this.ControlRender.ActiveRender != this ? this.ControlRender.ActiveRender.GetIdAtPixel(x, y) : vdgl.GetIdAtPixel(this.vdContext, x, y);

        /// <summary>
        /// Overrides the <see cref="M:VectorDraw.Render.vdRender.GetDepthAtPixel(System.Int32,System.Int32)" />
        /// </summary>
        public override double GetDepthAtPixel(int x, int y) => this.ControlRender != null && this.ControlRender.ActiveRender != null && this.ControlRender.ActiveRender != this ? this.ControlRender.ActiveRender.GetDepthAtPixel(x, y) : vdgl.GetDepthAtPixel(this.vdContext, x, y);

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.MatrixView2PixelChanged" />
        /// </summary>
        public override void MatrixView2PixelChanged()
        {
            base.MatrixView2PixelChanged();
            if (this.vdrawGlContext == IntPtr.Zero)
                return;
            int x = this.UpperLeft.X;
            int y = this.UpperLeft.Y;
            if (this.LayoutRender != null)
            {
                if (this.OwnerGraphicsOffset.X < 0)
                    x += this.OwnerGraphicsOffset.X;
                if (this.OwnerGraphicsOffset.Y < 0)
                    y += this.OwnerGraphicsOffset.Y;
            }
            vdgl.SetViewport(this.vdContext, x, y, this.Width, this.Height);
            vdgl.SetProjectionMatrix(this.vdContext, this.ProjectionMatrix);
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.MatrixViewChanged" />
        /// </summary>
        public override void MatrixViewChanged()
        {
            base.MatrixViewChanged();
            if (this.vdrawGlContext == IntPtr.Zero)
                return;
            VectorDraw.Geometry.Matrix currentMatrix = this.CurrentMatrix;
            vdgl.SetModelMatrix(this.vdContext, currentMatrix);
        }

        /// <summary>
        /// overrides the <see cref="P:VectorDraw.Render.vdRender.RenderMode" />
        /// </summary>
        public override vdRender.Mode RenderMode
        {
            get => base.RenderMode;
            set
            {
                base.RenderMode = value;
                if (!this.Started)
                    return;
                this.UpdateRenderModeProperties();
            }
        }

        /// <summary>
        /// overrides the <see cref="P:VectorDraw.Render.vdRender.ShowHidenEdges" />
        /// </summary>
        public override bool ShowHidenEdges
        {
            get => base.ShowHidenEdges;
            set
            {
                base.ShowHidenEdges = value;
                if (!(this.vdContext != IntPtr.Zero))
                    return;
                vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.SHOWHIDENEDGES, new double[1]
                {
          this.ShowHidenEdges ? 1.0 : 0.0
                });
            }
        }

        internal virtual void UpdateRenderModeProperties()
        {
            if (this.vdContext == IntPtr.Zero)
                return;
            vdglTypes.PropertyValues propertyValues1 = vdglTypes.PropertyValues.POLYGON_MODE_FILL;
            bool bVal = true;
            bool bvalue1 = true;
            bool bvalue2 = true;
            bool bvalue3 = false;
            vdglTypes.PropertyValues propertyValues2 = vdglTypes.PropertyValues.TEXTURE_OFF;
            double num1 = 0.0;
            double num2 = 0.0;
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.DRAWEDGE_MODE, new double[1]
            {
        (double) vdglTypes.PropertyValues.TEXTURE_OFF.GetHashCode()
            });
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.SHOWHIDENEDGES, new double[1]
            {
        this.ShowHidenEdges ? 1.0 : 0.0
            });
            switch (this.RenderMode)
            {
                case vdRender.Mode.Wire2d:
                case vdRender.Mode.Wire2dGdiPlus:
                    if (this.GlobalProperties.Wire2dSectionClip == vdRenderGlobalProperties.Wire2dSectionClipFlag.Off)
                        num2 = 1.0;
                    propertyValues1 = vdglTypes.PropertyValues.POLYGON_MODE_LINES;
                    bvalue1 = false;
                    bVal = false;
                    propertyValues2 = vdglTypes.PropertyValues.TEXTURE_OFF;
                    break;
                case vdRender.Mode.Wire3d:
                    propertyValues1 = vdglTypes.PropertyValues.POLYGON_MODE_LINES;
                    bVal = false;
                    propertyValues2 = !this.SupportWire3d_Transparent ? vdglTypes.PropertyValues.TEXTURE_OFF : vdglTypes.PropertyValues.POLYGON_MODE_LINES;
                    break;
                case vdRender.Mode.Hide:
                    propertyValues1 = vdglTypes.PropertyValues.POLYGON_MODE_FILL;
                    bVal = false;
                    bvalue2 = false;
                    propertyValues2 = vdglTypes.PropertyValues.TEXTURE_OFF;
                    vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.DRAWEDGE_MODE, new double[1]
                    {
            (double) vdglTypes.PropertyValues.POLYGON_MODE_LINES.GetHashCode()
                    });
                    break;
                case vdRender.Mode.Shade:
                    propertyValues1 = vdglTypes.PropertyValues.POLYGON_MODE_FILL;
                    propertyValues2 = vdglTypes.PropertyValues.POLYGON_MODE_LINES;
                    break;
                case vdRender.Mode.ShadeOn:
                    propertyValues1 = vdglTypes.PropertyValues.POLYGON_MODE_FILL;
                    propertyValues2 = vdglTypes.PropertyValues.POLYGON_MODE_LINES;
                    vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.DRAWEDGE_MODE, new double[1]
                    {
            (double) vdglTypes.PropertyValues.POLYGON_MODE_FILL.GetHashCode()
                    });
                    break;
                case vdRender.Mode.Render:
                    propertyValues1 = vdglTypes.PropertyValues.POLYGON_MODE_FILL;
                    propertyValues2 = vdglTypes.PropertyValues.POLYGON_MODE_LINES;
                    bvalue3 = true;
                    break;
                case vdRender.Mode.RenderOn:
                    propertyValues1 = vdglTypes.PropertyValues.POLYGON_MODE_FILL;
                    propertyValues2 = vdglTypes.PropertyValues.POLYGON_MODE_LINES;
                    bvalue3 = true;
                    vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.DRAWEDGE_MODE, new double[1]
                    {
            (double) vdglTypes.PropertyValues.POLYGON_MODE_FILL.GetHashCode()
                    });
                    break;
            }
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.POLYGONMODE, new double[1]
            {
        this.PolygonMode == vdRender.PolygonModeEnum.DEFAULT ? (double) propertyValues1.GetHashCode() : (double) this.PolygonMode.GetHashCode()
            });
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.DEPTH_OFFSET, new double[1]
            {
        num1
            });
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.TRANSPARENT_ORDER, new double[1]
            {
        (double) propertyValues2.GetHashCode()
            });
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.DISABLESECTIONS, new double[1]
            {
        num2
            });
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.EXTRALINETYPESCALE, new double[1]
            {
        this.ExtraLineTypeScale
            });
            if (this.ColorPalette == vdRender.ColorDisplay.BlackAndWhite)
                vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.ALPHAFUNC, new double[2]
                {
          1.0,
          (double) byte.MaxValue
                });
            else
                vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.ALPHAFUNC, new double[2]);
            this.EnableTexture(bvalue3);
            this.EnableDepthBuffer(bvalue1);
            this.EnableLighting(bVal);
            this.EnableColorBuffer(bvalue2);
            this.EnableBufferId(this.IsBufferIdEnable);
        }

        internal virtual void SetvdrawGLContextProperties()
        {
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.SHXBOLDWIDTH, new double[1]
            {
        (double) this.GlobalProperties.ShxBoldWidth
            });
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.MIN_PEN_WIDTH, new double[1]
            {
        this.IsPrinting || (double) this.GlobalProperties.MinPenWidth < 0.0 ? (double) this.GlobalProperties.MinPenWidth : 0.0
            });
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.FILLON_LINEWIDTH, new double[1]);
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.AMBIENT, new double[1]
            {
        38.25
            });
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.MATERIAL_LOCK, new double[1]
            {
        (double) vdglTypes.PropertyValues.TEXTURE_OFF.GetHashCode()
            });
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.RENDERINGHIGHTQUALITY, new double[1]
            {
        this.GlobalProperties.LineDrawQualityMode == vdRender.RenderingQualityMode.HighQuality || this.GlobalProperties.RenderingQuality == vdRender.RenderingQualityMode.HighQuality ? 1.0 : 0.0
            });
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.PENCAPSSQUARE, new double[1]
            {
        vdRender.PenCapsSquare ? 1.0 : 0.0
            });
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.EDGECOLOR, new double[4]);
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.LIGHT_FILTER, new double[1]
            {
        (double) this.GlobalProperties.LightsFilter.GetHashCode()
            });
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.ALPHAFUNC, new double[2]);
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.IGNORE_TRANSPARENCY, new double[1]
            {
        this.GlobalProperties.IgnoreTransparency ? 1.0 : 0.0
            });
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.TOTALCLIPCOUNTOURS, new double[1]
            {
        (double) grHatch.TotalClipCountours
            });
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.TOTALCOUNTOURSEGMENTS, new double[1]
            {
        (double) grHatch.TotalCountourSegments
            });
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.PARALLELFILL, new double[1]
            {
        this.GlobalProperties.ParallelFill ? 1.0 : 0.0
            });
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.DPIPATTERNOFFSET, new double[1]
            {
        this.GlobalProperties.DPIPatternOffset
            });
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.OPENGLFLAGS, new double[1]
            {
        (double) this.GlobalProperties.OpenGLFlags
            });
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.LINEWEIGHTOFF, new double[1]
            {
        !this.GlobalProperties.LineWeightDisplay ? 1.0 : 0.0
            });
            this.UpdateRenderModeProperties();
        }

        /// <summary>
        /// overrides the <see cref="P:VectorDraw.Render.vdRender.PolygonMode" />
        /// </summary>
        public override vdRender.PolygonModeEnum PolygonMode
        {
            get => base.PolygonMode;
            set
            {
                base.PolygonMode = value;
                if (this.PolygonMode == vdRender.PolygonModeEnum.DEFAULT)
                {
                    if (!this.IsWire2d && (this.RenderMode == vdRender.Mode.Hide || this.RenderMode == vdRender.Mode.Render || this.RenderMode == vdRender.Mode.Shade || this.RenderMode == vdRender.Mode.ShadeOn || this.RenderMode == vdRender.Mode.RenderOn))
                        vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.POLYGONMODE, new double[1]
                        {
              (double) vdglTypes.PropertyValues.POLYGON_MODE_FILL.GetHashCode()
                        });
                    else
                        vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.POLYGONMODE, new double[1]
                        {
              (double) vdglTypes.PropertyValues.POLYGON_MODE_LINES.GetHashCode()
                        });
                }
                else
                    vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.POLYGONMODE, new double[1]
                    {
            (double) this.PolygonMode.GetHashCode()
                    });
            }
        }

        private void EnsureContext()
        {
            if (!(this.vdrawGlContext == IntPtr.Zero))
                return;
            this.vdrawGlContext = vdgl.CreateContext();
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.StartDraw(System.Boolean)" />
        /// </summary>
        public override void StartDraw(bool updateproperties)
        {
            this.DrawWireStack.Push(false);
            this.SetvdrawGLContextProperties();
            this.LManager.Clear();
            base.StartDraw(updateproperties);
            if (!this.Started)
                return;
            if (this.SupportStringDrawRegistration)
                this.RegisterFunction(vdglTypes.FunctionType.DrawElement_String);
            if (this.Palette != null)
            {
                vdglTypes.SetPropertyValue_delegate setPropertyValue1 = vdgl.SetPropertyValue;
                IntPtr vdContext1 = this.vdContext;
                double[] PropertyValue1 = new double[4]
                {
          (double) this.Palette.Forground.R,
          0.0,
          0.0,
          0.0
                };
                Color color = this.Palette.Forground;
                PropertyValue1[1] = (double)color.G;
                color = this.Palette.Forground;
                PropertyValue1[2] = (double)color.B;
                color = this.Palette.Forground;
                PropertyValue1[3] = (double)color.A;
                setPropertyValue1(vdContext1, vdglTypes.PropertyType.FORGROUND, PropertyValue1);
                vdglTypes.SetPropertyValue_delegate setPropertyValue2 = vdgl.SetPropertyValue;
                IntPtr vdContext2 = this.vdContext;
                double[] PropertyValue2 = new double[4];
                color = this.Palette.Background;
                PropertyValue2[0] = (double)color.R;
                color = this.Palette.Background;
                PropertyValue2[1] = (double)color.G;
                color = this.Palette.Background;
                PropertyValue2[2] = (double)color.B;
                color = this.Palette.Background;
                PropertyValue2[3] = (double)color.A;
                setPropertyValue2(vdContext2, vdglTypes.PropertyType.BACKGROUND, PropertyValue2);
            }
            this.StatusDraw = vdRender.DrawStatus.Successed;
            this.PenStyleChanged((vdGdiPenStyle)null);
        }

        internal virtual bool SupportStringDrawRegistration
        {
            get
            {
                if (this.ControlRender != null && this.ControlRender.IsDepthBufferEnable || this.IsDepthBufferEnable)
                    return false;
                return this.Display == vdRender.DisplayMode.SCREEN || this.Display == vdRender.DisplayMode.SCREEN_BITMAP_PIXEL_FORMAT || this.Display == vdRender.DisplayMode.SCREEN_ACTION || this.Display == vdRender.DisplayMode.SCREEN_ACTION_HIGHLIGHT || this.IsPrinting;
            }
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.EndDraw" />
        /// </summary>
        public override void EndDraw()
        {
            if (!this.Started)
                return;
            this.UnRegisterFunction(vdglTypes.FunctionType.DrawElement_String);
            base.EndDraw();
            this.DrawWireStack.Pop();
        }

        internal virtual bool SupportEdgeRender => this.RenderMode == vdRender.Mode.Hide || this.RenderMode == vdRender.Mode.ShadeOn || this.RenderMode == vdRender.Mode.RenderOn;

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.StartEdgeRender" />
        /// </summary>
        public override bool StartEdgeRender()
        {
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.TRANSPARENT_ORDER, new double[1]
            {
        (double) vdglTypes.PropertyValues.TEXTURE_OFF.GetHashCode()
            });
            if (this.IsSelectingMode || !this.SupportEdgeRender)
                return false;
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.POLYGONMODE, new double[1]
            {
        (double) vdglTypes.PropertyValues.POLYGON_MODE_LINES.GetHashCode()
            });
            this.EnableLighting(false);
            this.EnableColorBuffer(true);
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.DEPTH_OFFSET, new double[1]
            {
        -0.001
            });
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.TRANSPARENT_ORDER, new double[1]
            {
        (double) vdglTypes.PropertyValues.TEXTURE_OFF.GetHashCode()
            });
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.FILLON_LINEWIDTH, new double[1]
            {
        (double) this.DpiY * (double) vdRenderGlobalProperties.FixedShadeOnPenWidth
            });
            if (!vdRender.IsColorEmpty(this.EdgeColor))
            {
                vdglTypes.SetPropertyValue_delegate setPropertyValue = vdgl.SetPropertyValue;
                IntPtr vdContext = this.vdContext;
                double[] PropertyValue = new double[4]
                {
          (double) this.EdgeColor.R,
          0.0,
          0.0,
          0.0
                };
                Color edgeColor = this.EdgeColor;
                PropertyValue[1] = (double)edgeColor.G;
                edgeColor = this.EdgeColor;
                PropertyValue[2] = (double)edgeColor.B;
                PropertyValue[3] = (double)byte.MaxValue;
                setPropertyValue(vdContext, vdglTypes.PropertyType.EDGECOLOR, PropertyValue);
            }
            this.mIsDrawEdgeOn = true;
            return true;
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.StopEdgeRender" />
        /// </summary>
        public override void StopEdgeRender()
        {
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.FILLON_LINEWIDTH, new double[1]);
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.EDGECOLOR, new double[4]);
            this.mIsDrawEdgeOn = false;
            this.UpdateRenderModeProperties();
        }

        /// <summary>
        /// overrides the <see cref="P:VectorDraw.Render.vdRender.IsDrawEdgeOn" />
        /// </summary>
        public override bool IsDrawEdgeOn => this.mIsDrawEdgeOn;

        /// <summary>
        /// Returns the drawing context that controls the pixelization to the output device.
        /// </summary>
        public IntPtr vdContext => this.vdrawGlContext;

        /// <summary>Initializes the object.</summary>
        public vdrawglRender(vdRender OriginalRender)
          : base(OriginalRender)
        {
            this.EnsureContext();
        }

        /// <summary>
        /// overrides the <see cref="P:VectorDraw.Render.vdRender.IsBlendingOn" />
        /// </summary>
        public override bool IsBlendingOn => this.mIsBlenOn;

        /// <summary>
        ///  overrides the <see cref="M:VectorDraw.Render.vdRender.SetTransparentOn" />
        /// </summary>
        public override void SetTransparentOn() => vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.HAS_TRANSPARENT, new double[1]
        {
      1.0
        });

        internal virtual bool SupportAlphaBlending
        {
            get
            {
                if (this.GlobalProperties.IgnoreTransparency)
                    return false;
                if (this.RenderMode == vdRender.Mode.Render || this.RenderMode == vdRender.Mode.Shade || this.RenderMode == vdRender.Mode.ShadeOn || this.RenderMode == vdRender.Mode.RenderOn)
                    return true;
                return this.RenderMode == vdRender.Mode.Wire3d && this.SupportWire3d_Transparent;
            }
        }

        /// <summary>
        /// Internally used.overrides the <see cref="M:VectorDraw.Render.vdRender.ForceBlending(System.Boolean)" />
        /// </summary>
        public override bool ForceBlending(bool bForce)
        {
            bool mForceBlending = this.mForceBlending;
            this.mForceBlending = bForce;
            if (this.mForceBlending)
                vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.TRANSPARENT_ORDER, new double[1]
                {
          (double) vdglTypes.PropertyValues.TEXTURE_OFF.GetHashCode()
                });
            else
                this.UpdateRenderModeProperties();
            return mForceBlending;
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.StartBlendingRender" />
        /// </summary>
        public override bool StartBlendingRender()
        {
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.TRANSPARENT_ORDER, new double[1]
            {
        (double) vdglTypes.PropertyValues.TEXTURE_OFF.GetHashCode()
            });
            if (!this.SupportAlphaBlending || this.ColorPalette == vdRender.ColorDisplay.BlackAndWhite || this.LockPenStyle != (vdGdiPenStyle)null && this.LockPenStyle.AlphaBlending == byte.MaxValue)
                return false;
            if (this.vdContext != IntPtr.Zero)
            {
                double PropertyValue = 0.0;
                vdgl.GetPropertyValue(this.vdContext, vdglTypes.PropertyTypeGet.HAS_TRANSPARENT, ref PropertyValue);
                if (((int)PropertyValue & 1) == 0)
                    return false;
                vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.TRANSPARENT_ORDER, new double[1]
                {
          (double) vdglTypes.PropertyValues.POLYGON_MODE_FILL.GetHashCode()
                });
            }
            this.mIsBlenOn = true;
            return true;
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.SetBlendDrawMode(System.Boolean)" />
        /// </summary>
        public override void SetBlendDrawMode(bool isFront)
        {
            if (isFront)
            {
                this.EnableColorBuffer(false);
                vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.TRANSPARENT_ORDER, new double[1]
                {
          (double) vdglTypes.PropertyValues.POLYGON_MODE_FILL.GetHashCode()
                });
            }
            else
            {
                this.EnableColorBuffer(true);
                vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.TRANSPARENT_ORDER, new double[1]
                {
          (double) vdglTypes.PropertyValues.TRANSPARENT_ORDER_ON_2.GetHashCode()
                });
            }
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.StopBlendingRender" />
        /// </summary>
        public override void StopBlendingRender()
        {
            this.mIsBlenOn = false;
            this.UpdateRenderModeProperties();
        }

        internal IntPtr BindImage(ImageBind image) => image == null ? IntPtr.Zero : image.GetBindPtr(this.vdContext);

        private bool ActiveDrawWire => this.DrawWireStack.Peek();

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.PushDrawFigureList(System.Object)" />
        /// </summary>
        public override vdRender.DrawStatus PushDrawFigureList(object obj)
        {
            bool flag = false;
            uint Id = 0;
            if (obj is IRenderListItem)
            {
                Id = ((IRenderListItem)obj).PixelIDFlag;
                flag = (((IRenderListItem)obj).Draw3DFlag & Draw3DFlagEnum.DrawWire) != 0;
            }
            vdgl.SetActiveId(this.vdContext, Id);
            if (!flag)
                flag = this.ActiveDrawWire;
            this.DrawWireStack.Push(flag);
            return base.PushDrawFigureList(obj);
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.PopDrawFigureList" />
        /// </summary>
        public override vdRender.DrawStatus PopDrawFigureList()
        {
            this.DrawWireStack.Pop();
            vdRender.DrawStatus drawStatus = base.PopDrawFigureList();
            object obj = (object)null;
            uint Id = 0;
            if (this.DrawFigureList.Count > 0)
                obj = this.DrawFigureList.Peek();
            if (obj is IRenderListItem)
                Id = ((IRenderListItem)obj).PixelIDFlag;
            vdgl.SetActiveId(this.vdContext, Id);
            return drawStatus;
        }

        internal virtual IntPtr ElementInit(object item, vdglTypes.FLAG_ELEMENT Flag, int UserId)
        {
            if (this.ActiveDrawWire)
            {
                Flag |= vdglTypes.FLAG_ELEMENT.FILL_ON | vdglTypes.FLAG_ELEMENT.FILL_ALWAYS;
                Flag ^= vdglTypes.FLAG_ELEMENT.FILL_ON | vdglTypes.FLAG_ELEMENT.FILL_ALWAYS;
            }
            Flag |= (vdglTypes.FLAG_ELEMENT)this.ElementFlagEx;
            return vdgl.ElementInit(this.vdContext, Flag, UserId);
        }

        internal virtual void ElementDraw(IntPtr contextPtr, IntPtr ElementPtr) => vdgl.ElementDraw(contextPtr, ElementPtr);

        internal virtual void ElementSetNormal(IntPtr contextPtr, IntPtr ElementPtr, Vector pt) => vdgl.ElementSetNormal(contextPtr, ElementPtr, pt.x, pt.y, pt.z);

        internal virtual void ElementSetVertex(
          IntPtr contextPtr,
          IntPtr ElementPtr,
          gPoint pt,
          vdglTypes.FLAG_VERTEX Flag,
          int UserId)
        {
            vdgl.ElementSetVertex(contextPtr, ElementPtr, pt.x, pt.y, pt.z, Flag, UserId);
        }

        internal virtual void ElementSetTexture(IntPtr contextPtr, IntPtr ElementPtr, gPoint pt) => vdgl.ElementSetTexture(contextPtr, ElementPtr, pt.x, pt.y, pt.z);

        internal virtual void ElementSetColorEx(IntPtr contextPtr, IntPtr ElementPtr, Color color) => vdgl.ElementSetColor(contextPtr, ElementPtr, color.R, color.G, color.B, color.A);

        /// <summary>
        /// Get the System color of the active color of the render.
        /// </summary>
        public override Color SystemPenColor => this.GetFinalColor(this.PenStyle.color, this.PenStyle.AlphaBlending);

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.GetFinalColor(System.Drawing.Color,System.Byte)" />
        /// </summary>
        public override Color GetFinalColor(Color color, byte AlphaBlending)
        {
            Color color1 = color;
            if (AlphaBlending != byte.MaxValue)
                color1 = vdGdiPenStyle.FromArgb(AlphaBlending, color1);
            return color1;
        }

        internal void SetActivePenStyle(bool checkUpdate, vdrawglRender.PenstyleFlag pflag)
        {
            if (checkUpdate)
            {
                if (!this.mPenStyleNeedUpdate)
                    return;
                this.mPenStyleNeedUpdate = false;
            }
            vdGdiPenStyle penStyle = this.PenStyle;
            IntPtr ImageId = this.BindImage(penStyle.MaterialBind);
            IntPtr linetypeId = this.BindLineType(penStyle.LineType);
            Color color = penStyle.gradientColor2;
            if (penStyle.gradientColor2 == Color.Empty)
                color = this.BkColor;
            Color systemPenColor = this.SystemPenColor;
            vdglTypes.MATERIAL_FLAG materialFlag = vdglTypes.MATERIAL_FLAG.NONE;
            if (penStyle.IsForgroundColor)
                materialFlag |= vdglTypes.MATERIAL_FLAG.COLORISFORGROUND;
            else if (penStyle.IsBackgroundColor)
                materialFlag |= vdglTypes.MATERIAL_FLAG.COLORISBACKGROUND;
            vdgl.SetMaterial(this.vdContext, (int)systemPenColor.R, (int)systemPenColor.G, (int)systemPenColor.B, (int)systemPenColor.A, (vdglTypes.PenWidthFlag)penStyle.PenWidthTypeProp, penStyle.PenWidth, linetypeId, penStyle.LineTypeScale, ImageId, vdgl.AsvdrawContextMatrix(penStyle.MaterialMatrix), (int)penStyle.gradientTypeProp, (int)color.R, (int)color.G, (int)color.B, (vdglTypes.MATERIAL_FLAG)(penStyle.ByBlockProperties | (vdGdiPenStyle.ByblockTypeEnum)materialFlag | (vdGdiPenStyle.ByblockTypeEnum)pflag));
        }

        internal override bool PenStylePushed(vdGdiPenStyle prevstyle)
        {
            if (!this.IsCreatingList)
                return base.PenStylePushed(prevstyle);
            this.SetActivePenStyle(false, vdrawglRender.PenstyleFlag.pushed);
            return true;
        }

        internal override bool PenStylePoped(vdGdiPenStyle prevstyle)
        {
            if (!this.IsCreatingList)
                return base.PenStylePoped(prevstyle);
            this.SetActivePenStyle(false, vdrawglRender.PenstyleFlag.poped);
            return true;
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.PenStyleChanged(VectorDraw.Render.vdGdiPenStyle)" />
        /// </summary>
        public override void PenStyleChanged(vdGdiPenStyle previousPen)
        {
            if (this.mPenStyleDisableWrite)
                return;
            this.mPenStyleNeedUpdate = true;
            this.SetActivePenStyle(true, vdrawglRender.PenstyleFlag.None);
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.FadeEffectChanged" />
        /// </summary>
        public override void FadeEffectChanged() => vdgl.SetFadeEffect(this.vdContext, (int)this.FadeEffect);

        /// <summary>
        /// overrides the <see cref="P:VectorDraw.Render.vdRender.LockPenStyle" />
        /// </summary>
        public override vdGdiPenStyle LockPenStyle
        {
            get => base.LockPenStyle;
            set
            {
                vdglTypes.PropertyValues propertyValues;
                if (value == (vdGdiPenStyle)null)
                {
                    vdglTypes.SetPropertyValue_delegate setPropertyValue = vdgl.SetPropertyValue;
                    IntPtr vdContext = this.vdContext;
                    double[] PropertyValue = new double[1];
                    propertyValues = vdglTypes.PropertyValues.TEXTURE_OFF;
                    PropertyValue[0] = (double)propertyValues.GetHashCode();
                    setPropertyValue(vdContext, vdglTypes.PropertyType.MATERIAL_LOCK, PropertyValue);
                }
                base.LockPenStyle = value;
                if (!(value != (vdGdiPenStyle)null))
                    return;
                vdglTypes.SetPropertyValue_delegate setPropertyValue1 = vdgl.SetPropertyValue;
                IntPtr vdContext1 = this.vdContext;
                double[] PropertyValue1 = new double[1];
                propertyValues = vdglTypes.PropertyValues.POLYGON_MODE_LINES;
                PropertyValue1[0] = (double)propertyValues.GetHashCode();
                setPropertyValue1(vdContext1, vdglTypes.PropertyType.MATERIAL_LOCK, PropertyValue1);
            }
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.OnUpdateClipping(System.Boolean)" />
        /// </summary>
        public override void OnUpdateClipping(bool ispop)
        {
            if (this.IsLock || this.graphics == null)
                return;
            System.Drawing.Region gdiPlusRegion = this.ClippingRegion.GetGdiPlusRegion(this.View2PixelMatrix, this.ViewCenter.z + this.FocalLength);
            if (gdiPlusRegion == null)
            {
                this.graphics.ResetClip();
            }
            else
            {
                this.graphics.SetClip(gdiPlusRegion, CombineMode.Replace);
                gdiPlusRegion.Dispose();
            }
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.Refresh(System.Drawing.Graphics,System.Int32,System.Int32)" />
        /// </summary>
        public override void Refresh(Graphics gr, int x, int y)
        {
            if (gr == null)
                gr = this.graphics;
            if (gr == null || this.MemoryBitmap == null)
                return;
            this.mIsQuickLock = true;
            bool isLock = this.IsLock;
            if (isLock)
                this.UnLock();
            if (this.graphics != gr)
                vdRender.DrawImageUnscaled(gr, (Image)this.MemoryBitmap, x, y);
            if (isLock)
                this.Lock();
            this.mIsQuickLock = false;
        }

        /// <summary>
        /// overrides the <see cref="P:VectorDraw.Render.vdRender.SupportLights" />
        /// </summary>
        public override bool SupportLights => this.RenderMode == vdRender.Mode.Shade || this.RenderMode == vdRender.Mode.ShadeOn || this.RenderMode == vdRender.Mode.Render || this.RenderMode == vdRender.Mode.RenderOn;

        /// <summary>
        /// Implements the <see cref="P:VectorDraw.Render.IRenderList.IsSelectingMode" />
        /// </summary>
        public override bool IsSelectingMode => false;

        internal bool RegisterFunction(vdglTypes.FunctionType FType)
        {
            switch (FType)
            {
                case vdglTypes.FunctionType.DrawElement:
                    if (this.DrawElementFunc != null)
                        return true;
                    this.DrawElementFunc = Delegate.CreateDelegate(typeof(vdglTypes.DrawElementDelegate), (object)this, "OnDrawElement") as vdglTypes.DrawElementDelegate;
                    vdgl.SetFunctionOverride(this.vdContext, FType, Marshal.GetFunctionPointerForDelegate((Delegate)this.DrawElementFunc));
                    break;
                case vdglTypes.FunctionType.DrawElementSucced:
                    if (this.DrawElementSuccedFunc != null)
                        return true;
                    this.DrawElementSuccedFunc = Delegate.CreateDelegate(typeof(vdglTypes.DrawElementSuccedDelegate), (object)this, "OnDrawElementSucced") as vdglTypes.DrawElementSuccedDelegate;
                    vdgl.SetFunctionOverride(this.vdContext, FType, Marshal.GetFunctionPointerForDelegate((Delegate)this.DrawElementSuccedFunc));
                    break;
                case vdglTypes.FunctionType.ENUM_IMAGE_BIND:
                    if (this.ImageBindFunc != null)
                        return true;
                    this.ImageBindFunc = Delegate.CreateDelegate(typeof(vdglTypes.ImageBindDelegate), (object)this, "OnImageBind") as vdglTypes.ImageBindDelegate;
                    vdgl.SetFunctionOverride(this.vdContext, FType, Marshal.GetFunctionPointerForDelegate((Delegate)this.ImageBindFunc));
                    break;
                case vdglTypes.FunctionType.DrawElement_String:
                    if (this.DrawElement_StringFunc != null)
                        return true;
                    this.DrawElement_StringFunc = Delegate.CreateDelegate(typeof(vdglTypes.DrawElement_StringDelegate), (object)this, "OnDrawElement_String") as vdglTypes.DrawElement_StringDelegate;
                    vdgl.SetFunctionOverride(this.vdContext, FType, Marshal.GetFunctionPointerForDelegate((Delegate)this.DrawElement_StringFunc));
                    break;
                case vdglTypes.FunctionType.PUSHALIGNTOVIEW:
                    if (this.PushAlignToViewFunc != null)
                        return true;
                    this.PushAlignToViewFunc = Delegate.CreateDelegate(typeof(vdglTypes.PushAlignToViewDelegate), (object)this, "OnPushAlignToView") as vdglTypes.PushAlignToViewDelegate;
                    vdgl.SetFunctionOverride(this.vdContext, FType, Marshal.GetFunctionPointerForDelegate((Delegate)this.PushAlignToViewFunc));
                    break;
                case vdglTypes.FunctionType.POPALIGNTOVIEW:
                    if (this.PopAlignToViewFunc != null)
                        return true;
                    this.PopAlignToViewFunc = Delegate.CreateDelegate(typeof(vdglTypes.PopAlignToViewDelegate), (object)this, "OnPopAlignToView") as vdglTypes.PopAlignToViewDelegate;
                    vdgl.SetFunctionOverride(this.vdContext, FType, Marshal.GetFunctionPointerForDelegate((Delegate)this.PopAlignToViewFunc));
                    break;
                case vdglTypes.FunctionType.ENUM_IMAGE_BIND_CREATED:
                    if (this.ImageBindCreatedFunc != null)
                        return true;
                    this.ImageBindCreatedFunc = Delegate.CreateDelegate(typeof(vdglTypes.ImageBindCreatedDelegate), (object)this, "OnImageBindCreated") as vdglTypes.ImageBindCreatedDelegate;
                    vdgl.SetFunctionOverride(this.vdContext, FType, Marshal.GetFunctionPointerForDelegate((Delegate)this.ImageBindCreatedFunc));
                    break;
                case vdglTypes.FunctionType.DRAWARRAYS:
                    if (this.DrawArraysFunc != null)
                        return true;
                    this.DrawArraysFunc = Delegate.CreateDelegate(typeof(vdglTypes.PFNONDRAWARRAYSPROC), (object)this, "OnDrawArrays") as vdglTypes.PFNONDRAWARRAYSPROC;
                    vdgl.SetFunctionOverride(this.vdContext, FType, Marshal.GetFunctionPointerForDelegate((Delegate)this.DrawArraysFunc));
                    break;
                case vdglTypes.FunctionType.DRAWMESH:
                    if (this.DrawMeshFunc != null)
                        return true;
                    this.DrawMeshFunc = Delegate.CreateDelegate(typeof(vdglTypes.PFNONMESHPROC), (object)this, "OnDrawMesh") as vdglTypes.PFNONMESHPROC;
                    vdgl.SetFunctionOverride(this.vdContext, FType, Marshal.GetFunctionPointerForDelegate((Delegate)this.DrawMeshFunc));
                    break;
                default:
                    return false;
            }
            return false;
        }

        internal void UnRegisterFunction(vdglTypes.FunctionType FType)
        {
            switch (FType)
            {
                case vdglTypes.FunctionType.DrawElement:
                    if (this.DrawElementFunc == null)
                        break;
                    vdgl.SetFunctionOverride(this.vdContext, vdglTypes.FunctionType.DrawElement, IntPtr.Zero);
                    this.DrawElementFunc = (vdglTypes.DrawElementDelegate)null;
                    break;
                case vdglTypes.FunctionType.DrawElementSucced:
                    if (this.DrawElementSuccedFunc == null)
                        break;
                    vdgl.SetFunctionOverride(this.vdContext, vdglTypes.FunctionType.DrawElementSucced, IntPtr.Zero);
                    this.DrawElementSuccedFunc = (vdglTypes.DrawElementSuccedDelegate)null;
                    break;
                case vdglTypes.FunctionType.ENUM_IMAGE_BIND:
                    if (this.ImageBindFunc == null)
                        break;
                    vdgl.SetFunctionOverride(this.vdContext, vdglTypes.FunctionType.ENUM_IMAGE_BIND, IntPtr.Zero);
                    this.ImageBindFunc = (vdglTypes.ImageBindDelegate)null;
                    break;
                case vdglTypes.FunctionType.DrawElement_String:
                    if (this.DrawElement_StringFunc == null)
                        break;
                    vdgl.SetFunctionOverride(this.vdContext, vdglTypes.FunctionType.DrawElement_String, IntPtr.Zero);
                    this.DrawElement_StringFunc = (vdglTypes.DrawElement_StringDelegate)null;
                    break;
                case vdglTypes.FunctionType.PUSHALIGNTOVIEW:
                    if (this.PushAlignToViewFunc == null)
                        break;
                    vdgl.SetFunctionOverride(this.vdContext, vdglTypes.FunctionType.PUSHALIGNTOVIEW, IntPtr.Zero);
                    this.PushAlignToViewFunc = (vdglTypes.PushAlignToViewDelegate)null;
                    break;
                case vdglTypes.FunctionType.POPALIGNTOVIEW:
                    if (this.PopAlignToViewFunc == null)
                        break;
                    vdgl.SetFunctionOverride(this.vdContext, vdglTypes.FunctionType.POPALIGNTOVIEW, IntPtr.Zero);
                    this.PopAlignToViewFunc = (vdglTypes.PopAlignToViewDelegate)null;
                    break;
                case vdglTypes.FunctionType.ENUM_IMAGE_BIND_CREATED:
                    if (this.ImageBindCreatedFunc == null)
                        break;
                    vdgl.SetFunctionOverride(this.vdContext, vdglTypes.FunctionType.ENUM_IMAGE_BIND_CREATED, IntPtr.Zero);
                    this.ImageBindCreatedFunc = (vdglTypes.ImageBindCreatedDelegate)null;
                    break;
                case vdglTypes.FunctionType.DRAWARRAYS:
                    if (this.DrawArraysFunc == null)
                        break;
                    vdgl.SetFunctionOverride(this.vdContext, vdglTypes.FunctionType.DRAWARRAYS, IntPtr.Zero);
                    this.DrawArraysFunc = (vdglTypes.PFNONDRAWARRAYSPROC)null;
                    break;
                case vdglTypes.FunctionType.DRAWMESH:
                    if (this.DrawMeshFunc == null)
                        break;
                    vdgl.SetFunctionOverride(this.vdContext, vdglTypes.FunctionType.DRAWMESH, IntPtr.Zero);
                    this.DrawMeshFunc = (vdglTypes.PFNONMESHPROC)null;
                    break;
            }
        }

        /// <summary>
        /// overrides the <see cref="P:VectorDraw.Render.vdRender.IsContextCreated" />
        /// </summary>
        public override bool IsContextCreated => this.vdContext != IntPtr.Zero;

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.Lock" />
        /// </summary>
        public override void Lock()
        {
            if (this.IsLock)
                return;
            this.mPenStyleNeedUpdate = true;
            base.Lock();
            if (this.MemoryBitmap != null)
            {
                this.bmpData = this.MemoryBitmap.LockBits(new Rectangle(0, 0, this.MemoryBitmap.Width, this.MemoryBitmap.Height), ImageLockMode.ReadWrite, BitmapWrapper.DefaultPixelFormat);
                if (!this.mIsQuickLock)
                {
                    if (this is ActionWrapperRender)
                    {
                        vdgl.SetBitmapContext(this.vdContext, this.MemoryBitmap.Width, this.MemoryBitmap.Height, this.bmpData.Scan0, new int[9]
                        {
              0,
              0,
              this.MemoryBitmap.Width,
              this.MemoryBitmap.Height,
              this.UpperLeft.X,
              this.UpperLeft.Y,
              this.Width,
              this.Height,
              1
                        }, (double)this.DpiY);
                    }
                    else
                    {
                        vdglTypes.SetBitmapContext_delegate setBitmapContext = vdgl.SetBitmapContext;
                        IntPtr vdContext = this.vdContext;
                        int width = this.MemoryBitmap.Width;
                        int height = this.MemoryBitmap.Height;
                        IntPtr scan0 = this.bmpData.Scan0;
                        int[] clipViewPort = new int[9];
                        clipViewPort[0] = this.UpperLeft.X;
                        clipViewPort[1] = this.UpperLeft.Y;
                        clipViewPort[2] = this.MemoryBitmap.Width;
                        clipViewPort[3] = this.MemoryBitmap.Height;
                        Point ownerGraphicsOffset = this.OwnerGraphicsOffset;
                        clipViewPort[4] = ownerGraphicsOffset.X;
                        ownerGraphicsOffset = this.OwnerGraphicsOffset;
                        clipViewPort[5] = ownerGraphicsOffset.Y;
                        clipViewPort[6] = this.Width;
                        clipViewPort[7] = this.Height;
                        double dpiY = (double)this.DpiY;
                        setBitmapContext(vdContext, width, height, scan0, clipViewPort, dpiY);
                    }
                    Rectangle ivalidateRect = this.IvalidateRect;
                    if (!ivalidateRect.IsEmpty)
                    {
                        vdglTypes.SetClipBox_delegate setClipBox = vdgl.SetClipBox;
                        IntPtr vdContext = this.vdContext;
                        ivalidateRect = this.IvalidateRect;
                        int left1 = ivalidateRect.Left;
                        ivalidateRect = this.IvalidateRect;
                        int top1 = ivalidateRect.Top;
                        ivalidateRect = this.IvalidateRect;
                        int right = ivalidateRect.Right;
                        ivalidateRect = this.IvalidateRect;
                        int left2 = ivalidateRect.Left;
                        int width = right - left2 + 1;
                        ivalidateRect = this.IvalidateRect;
                        int bottom = ivalidateRect.Bottom;
                        ivalidateRect = this.IvalidateRect;
                        int top2 = ivalidateRect.Top;
                        int height = bottom - top2 + 1;
                        setClipBox(vdContext, left1, top1, width, height);
                    }
                }
            }
            else if (!this.mIsQuickLock)
                vdgl.SetBitmapContext(this.vdContext, this.Width, this.Height, IntPtr.Zero, (int[])null, (double)this.DpiY);
            if (!this.mIsQuickLock && this.BreakOnMessage != MessageManager.BreakMessageMethod.None)
                this.RegisterFunction(vdglTypes.FunctionType.DrawElement);
            this.RegisterFunction(vdglTypes.FunctionType.PUSHALIGNTOVIEW);
            this.RegisterFunction(vdglTypes.FunctionType.POPALIGNTOVIEW);
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.UnLock" />
        /// </summary>
        public override void UnLock()
        {
            if (!this.IsLock)
                return;
            base.UnLock();
            if (!this.mIsQuickLock)
            {
                IntPtr zero = IntPtr.Zero;
                int size = 0;
                Rectangle DrawnBox = new Rectangle();
                this.OnFinish(ref zero, ref size, ref DrawnBox);
                this.UnRegisterFunction(vdglTypes.FunctionType.DrawElement);
                this.UnRegisterFunction(vdglTypes.FunctionType.DrawElementSucced);
            }
            if (this.MemoryBitmap != null && this.bmpData != null)
            {
                this.MemoryBitmap.UnlockBits(this.bmpData);
                this.bmpData = (BitmapData)null;
            }
            if (!this.mIsQuickLock)
                vdgl.SetBitmapContext(this.vdContext, 0, 0, IntPtr.Zero, (int[])null, (double)this.DpiY);
            this.UnRegisterFunction(vdglTypes.FunctionType.PUSHALIGNTOVIEW);
            this.UnRegisterFunction(vdglTypes.FunctionType.POPALIGNTOVIEW);
        }

        /// <summary>
        /// overrides the <see cref="P:VectorDraw.Render.vdRender.BreakOnMessage" />
        /// </summary>
        public override MessageManager.BreakMessageMethod BreakOnMessage
        {
            get => base.BreakOnMessage;
            set
            {
                base.BreakOnMessage = value;
                if (this.BreakOnMessage != MessageManager.BreakMessageMethod.None)
                    this.RegisterFunction(vdglTypes.FunctionType.DrawElement);
                else
                    this.UnRegisterFunction(vdglTypes.FunctionType.DrawElement);
            }
        }

        internal virtual void OnDrawElement(IntPtr Element, ref int Cancel)
        {
            if (this.TestDrawBreak())
                Cancel = 1;
            else
                this.TestTimerEvent();
        }

        internal virtual void OnDrawElementSucced(
          double[] modelmatrix,
          double minz,
          double DistanceFromCenter,
          int elementUserId,
          int vertexUserId,
          vdglTypes.SelectStatusCode statusCode,
          int isFill,
          ref int Cancel)
        {
        }

        internal virtual void OnImageBind(IntPtr image, vdglTypes.MATERIAL_FLAG materialFlag)
        {
        }

        internal virtual void OnImageBindCreated(IntPtr image, int Flag)
        {
        }

        internal virtual void OnDrawArrays(IntPtr vdcontext, IntPtr drawbufferPtr)
        {
        }

        internal virtual void OnDrawMesh(
          IntPtr contextPtr,
          int mesh_items,
          byte mesh_stride,
          double[] midpoint,
          IntPtr mesh_verts,
          IntPtr mesh_normals,
          IntPtr colors,
          IntPtr textures,
          IntPtr edges)
        {
        }

        internal virtual void OnPushAlignToView(
          byte Flag,
          double[] InsertionPoint,
          double[] ExtrusionVector,
          double[] modelmatrix,
          int AlignToViewSize,
          double objectHeight,
          double objectRotation)
        {
            this.PushAlignToViewMatrix((vdRender.MatrixPushFlag)Flag, new gPoint(InsertionPoint[0], InsertionPoint[1], InsertionPoint[2]), new Vector(ExtrusionVector[0], ExtrusionVector[1], ExtrusionVector[2]), vdgl.ReadMatrix(modelmatrix), AlignToViewSize, objectHeight, objectRotation);
        }

        internal virtual void OnPopAlignToView() => base.PopAlignToView();

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.PushAlignToView(VectorDraw.Render.vdRender.MatrixPushFlag,VectorDraw.Geometry.gPoint,VectorDraw.Geometry.Vector,System.Int32,System.Double,System.Double)" />
        /// </summary>
        public override bool PushAlignToView(
          vdRender.MatrixPushFlag AlignToView,
          gPoint InsertionPoint,
          Vector ExtrusionVector,
          int AlignToViewSize,
          double objectHeight,
          double objectRotation)
        {
            if (AlignToView == vdRender.MatrixPushFlag.None && ((double)AlignToViewSize == 0.0 || objectHeight <= 0.0))
                return false;
            vdgl.PushAlignToView(this.vdContext, (byte)AlignToView, InsertionPoint.x, InsertionPoint.y, InsertionPoint.z, ExtrusionVector.x, ExtrusionVector.y, ExtrusionVector.z, AlignToViewSize, objectHeight, objectRotation);
            return true;
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.PopAlignToView" />
        /// </summary>
        public override void PopAlignToView() => vdgl.PopAlignToView(this.vdContext);

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.ApplyColorPalette" />
        /// </summary>
        public override void ApplyColorPalette()
        {
            if (this.ColorPalette == vdRender.ColorDisplay.TrueColor || !this.IsLock || this.bmpData == null || this.bmpData.Stride / this.bmpData.Width != 4)
                return;
            vdgl.ApplyFilter(this.vdContext, this.bmpData.Scan0, this.bmpData.Width, this.bmpData.Height, this.ColorPalette.GetHashCode());
        }

        internal virtual int OnFinish(ref IntPtr bytes, ref int size, ref Rectangle DrawnBox)
        {
            int[] DrawnBox1 = new int[4];
            int num = vdgl.Finish(this.vdContext, ref bytes, ref size, DrawnBox1);
            DrawnBox.X = DrawnBox1[0];
            DrawnBox.Y = DrawnBox1[1];
            DrawnBox.Width = DrawnBox1[2] - DrawnBox1[0];
            DrawnBox.Height = DrawnBox1[3] - DrawnBox1[1];
            return num;
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.Destroy(System.Boolean)" />
        /// </summary>
        public override void Destroy(bool bFinalized)
        {
            base.Destroy(bFinalized);
            if (this.mDestoyed)
                return;
            this.mDestoyed = true;
            if (this.vdrawGlContext != IntPtr.Zero)
            {
                this.UnRegisterFunction(vdglTypes.FunctionType.DrawElement);
                this.UnRegisterFunction(vdglTypes.FunctionType.DrawElement_String);
                this.UnRegisterFunction(vdglTypes.FunctionType.DrawElementSucced);
                this.UnRegisterFunction(vdglTypes.FunctionType.ENUM_IMAGE_BIND);
                this.UnRegisterFunction(vdglTypes.FunctionType.ENUM_IMAGE_BIND_CREATED);
                vdgl.DeleteContext(ref this.vdrawGlContext);
            }
            this.LManager.Dispose();
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.ClearDepthBuffer" />
        /// </summary>
        public override void ClearDepthBuffer() => vdgl.ClearContext(this.vdContext, 0, 0, 0, 0, 0, 0, 0, 0, vdglTypes.FLAG_CLEAR.DEPTH_BUFFER, IntPtr.Zero, 0, 0, 0, 0);

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.Clear(System.Drawing.Color,System.Boolean)" />
        /// </summary>
        public override void Clear(Color color, bool applyGradient)
        {
            base.Clear(color, applyGradient);
            bool flag = applyGradient && !vdRender.IsColorEmpty(this.BkGradientColor);
            vdglTypes.FLAG_CLEAR flagClear = (vdglTypes.FLAG_CLEAR)(2 | 1);
            if (flag)
                flagClear |= vdglTypes.FLAG_CLEAR.GRADIENT_COLOR;
            Color color1;
            if (this.Palette != null)
            {
                if (!vdRender.IsColorEmpty(color))
                    this.Palette.SetBkColorFixForground(this.BkColor);
                vdglTypes.SetPropertyValue_delegate setPropertyValue1 = vdgl.SetPropertyValue;
                IntPtr vdContext1 = this.vdContext;
                double[] PropertyValue1 = new double[4]
                {
          (double) this.Palette.Forground.R,
          (double) this.Palette.Forground.G,
          (double) this.Palette.Forground.B,
          0.0
                };
                Color color2 = this.Palette.Forground;
                PropertyValue1[3] = (double)color2.A;
                setPropertyValue1(vdContext1, vdglTypes.PropertyType.FORGROUND, PropertyValue1);
                vdglTypes.SetPropertyValue_delegate setPropertyValue2 = vdgl.SetPropertyValue;
                IntPtr vdContext2 = this.vdContext;
                double[] PropertyValue2 = new double[4];
                color2 = this.Palette.Background;
                PropertyValue2[0] = (double)color2.R;
                color2 = this.Palette.Background;
                PropertyValue2[1] = (double)color2.G;
                color2 = this.Palette.Background;
                PropertyValue2[2] = (double)color2.B;
                color1 = this.Palette.Background;
                PropertyValue2[3] = (double)color1.A;
                setPropertyValue2(vdContext2, vdglTypes.PropertyType.BACKGROUND, PropertyValue2);
            }
            if (vdRender.IsColorEmpty(color))
                color = this.LayoutRender == null || this.LayoutRender.MemoryBitmap != null ? Color.FromArgb(0, this.Palette.Background) : this.BkColor;
            vdglTypes.ClearContext_delegate clearContext = vdgl.ClearContext;
            IntPtr vdContext = this.vdContext;
            int r1 = (int)color.R;
            int g1 = (int)color.G;
            int b1 = (int)color.B;
            int a = (int)color.A;
            color1 = this.BkGradientColor;
            int r2 = (int)color1.R;
            color1 = this.BkGradientColor;
            int g2 = (int)color1.G;
            color1 = this.BkGradientColor;
            int b2 = (int)color1.B;
            int degrees = (int)Globals.RadiansToDegrees(this.BkGradientAngle);
            int Flag = (int)flagClear;
            IntPtr zero = IntPtr.Zero;
            clearContext(vdContext, r1, g1, b1, a, r2, g2, b2, degrees, (vdglTypes.FLAG_CLEAR)Flag, zero, 0, 0, 0, 0);
        }

        /// <summary>
        /// overrides the <see cref="P:VectorDraw.Render.vdRender.DpiY" />
        /// </summary>
        public override float DpiY => this.graphics == null ? base.DpiY : this.graphics.DpiY;

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.DrawLight(System.Object,VectorDraw.Render.IRenderingLight)" />
        /// </summary>
        public override void DrawLight(object sender, IRenderingLight light)
        {
            base.DrawLight(sender, light);
            int lightIndex = this.LManager.GetLightIndex(light);
            if (lightIndex < 0)
                return;
            vdglTypes.SetLightProps_delegate setLightProps = vdgl.SetLightProps;
            IntPtr vdContext = this.vdContext;
            int lightId = lightIndex;
            int Enable = light.Enable ? 1 : 0;
            int typeOfLight = (int)light.TypeOfLight;
            double Intensity = (double)light.IntensityValue / (double)byte.MaxValue;
            double x1 = light.Position.x;
            double y1 = light.Position.y;
            double z1 = light.Position.z;
            double x2 = light.Direction.x;
            double y2 = light.Direction.y;
            double z2 = light.Direction.z;
            int r = (int)light.color.R;
            Color color = light.color;
            int g = (int)color.G;
            color = light.color;
            int b = (int)color.B;
            double spotFallOff = light.SpotFallOff;
            double spotAngle = light.SpotAngle;
            setLightProps(vdContext, lightId, Enable, typeOfLight, Intensity, x1, y1, z1, x2, y2, z2, r, g, b, spotFallOff, spotAngle);
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.ClearAllSectionClips" />
        /// </summary>
        public override void ClearAllSectionClips()
        {
            if (this.vdContext != IntPtr.Zero)
            {
                for (int sectionId = 0; sectionId < 6; ++sectionId)
                    vdgl.SetSectionProps(this.vdContext, sectionId, 0, 0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0);
            }
            base.ClearAllSectionClips();
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.ApplySectionClips(VectorDraw.Geometry.ISectionClips)" />
        /// </summary>
        public override void ApplySectionClips(ISectionClips sections)
        {
            base.ApplySectionClips(sections);
            if (sections == null)
                return;
            int sectionId = 0;
            for (int index = 0; index < sections.Count && sectionId <= 5; ++index)
            {
                ISectionClip sectionClip = sections.GetItem(index);
                if (sectionClip.Enable)
                {
                    vdgl.SetSectionProps(this.vdContext, sectionId, sectionClip.Id, sectionClip.Enable ? 1 : 0, sectionClip.OriginPoint.x, sectionClip.OriginPoint.y, sectionClip.OriginPoint.z, sectionClip.Direction.x, sectionClip.Direction.y, sectionClip.Direction.z);
                    ++sectionId;
                }
            }
        }

        internal bool IsActiveHighLightFilterOn => this.ActiveHighLightFilter == vdRender.HighLightFilter.On;

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.PushHighLightFilter(VectorDraw.Render.vdRender.HighLightFilter)" />
        /// </summary>
        public override void PushHighLightFilter(vdRender.HighLightFilter nvalue)
        {
            base.PushHighLightFilter(nvalue);
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.HIGHLIGHT, new double[1]
            {
        this.IsActiveHighLightFilterOn ? 1.0 : 0.0
            });
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.PopHighLightFilter" />
        /// </summary>
        public override void PopHighLightFilter()
        {
            base.PopHighLightFilter();
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.HIGHLIGHT, new double[1]
            {
        this.IsActiveHighLightFilterOn ? 1.0 : 0.0
            });
        }

        /// <summary>
        /// Se to the vdRender a boolean value either to use a stipple filter or not. Striple filter is used for the SectionClip extended highlight filter.
        /// </summary>
        /// <param name="bval">True or false to enable this stipple filter.</param>
        /// <returns> the previous selected stipple. </returns>
        public override bool SetDrawStipple(bool bval)
        {
            bool mDrawStipple = this.mDrawStipple;
            this.mDrawStipple = bval;
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.POLYGON_STIPPLE, bval ? this.polygonStipple : (double[])null);
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.LINE_STIPPLE, bval ? this.lineStipple : (double[])null);
            return mDrawStipple;
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.DrawGdiBox(System.Drawing.Point,System.Int32)" />
        /// </summary>
        public override bool DrawGdiBox(Point center, int size)
        {
            if (this.TestDrawBreak())
                return false;
            this.SetActivePenStyle(true, vdrawglRender.PenstyleFlag.None);
            vdgl.DrawPixelBox(this.vdContext, (int)((double)center.X - (double)size * 0.5), (int)((double)center.Y - (double)size * 0.5), size, size, 0);
            return true;
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.PixelLineDraw(VectorDraw.Geometry.gPoint,VectorDraw.Geometry.gPoint)" />
        /// </summary>
        public override void PixelLineDraw(gPoint p1, gPoint p2) => this.DrawGdiLine((int)p1.x, (int)p1.y, p1.z, (int)p2.x, (int)p2.y, p2.z);

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.DrawGdiLine(System.Int32,System.Int32,System.Double,System.Int32,System.Int32,System.Double)" />
        /// </summary>
        public override void DrawGdiLine(int sx, int sy, double sz, int ex, int ey, double ez)
        {
            if (this.TestDrawBreak())
                return;
            this.SetActivePenStyle(true, vdrawglRender.PenstyleFlag.None);
            vdgl.DrawPixelLine(this.vdContext, sx, sy, sz, ex, ey, ez);
        }

        /// <summary>
        /// Push a polygon from the passed parameter as a clip boundary for the render.
        /// </summary>
        /// <param name="pts">A collection of points that represent the clip polygon in World coordinate system.</param>
        /// <returns>True if the clip was set succesfully.</returns>
        public override bool PushClipPolygon(gPoints pts)
        {
            if (pts == null || pts.Count < 3)
                return vdgl.PushClipPolygon(this.vdContext, (double[])null, 0) != 0;
            double[] pts1 = new double[pts.Count * 3];
            for (int index = 0; index < pts.Count; ++index)
            {
                pts1[index * 3] = pts[index].x;
                pts1[index * 3 + 1] = pts[index].y;
                pts1[index * 3 + 2] = pts[index].z;
            }
            return vdgl.PushClipPolygon(this.vdContext, pts1, pts.Count) != 0;
        }

        /// <summary>
        /// Use this method to pop the clip polygon that was set whit <see cref="M:VectorDraw.Render.vdrawglRender.PushClipPolygon(VectorDraw.Geometry.gPoints)" />
        /// </summary>
        public override void PopClipPolygon() => vdgl.PopClipPolygon(this.vdContext);

        /// <summary>
        /// Overrides the <see cref="M:VectorDraw.Render.vdRender.DrawLine(System.Object,VectorDraw.Geometry.gPoint,VectorDraw.Geometry.gPoint,System.Int32)" />
        /// </summary>
        public override void DrawLine(object sender, gPoint sp, gPoint ep, int segmentIndex)
        {
            if (this.TestDrawBreak())
                return;
            IntPtr ElementPtr = this.ElementInit(sender, vdglTypes.FLAG_ELEMENT.None, -1);
            if (ElementPtr != IntPtr.Zero)
            {
                this.ElementSetVertex(this.vdContext, ElementPtr, sp, vdglTypes.FLAG_VERTEX.None, segmentIndex);
                this.ElementSetVertex(this.vdContext, ElementPtr, ep, vdglTypes.FLAG_VERTEX.None, segmentIndex);
                this.ElementDraw(this.vdContext, ElementPtr);
            }
            this.TestTimerEvent();
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.DrawLine(System.Object,VectorDraw.Geometry.gPoint,VectorDraw.Geometry.gPoint)" />
        /// </summary>
        public override void DrawLine(object sender, gPoint sp, gPoint ep) => this.DrawLine(sender, sp, ep, -1);

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.DrawPLine(System.Object,VectorDraw.Geometry.gPoints)" />
        /// </summary>
        public override void DrawPLine(object sender, gPoints points)
        {
            if (this.TestDrawBreak())
                return;
            IntPtr ElementPtr = this.ElementInit(sender, vdglTypes.FLAG_ELEMENT.None, -1);
            if (ElementPtr != IntPtr.Zero)
            {
                if (points.SegmentPoints != null && points.SegmentPoints.Count == points.Count)
                {
                    for (int index = 0; index < points.Count; ++index)
                        this.ElementSetVertex(this.vdContext, ElementPtr, points[index], vdglTypes.FLAG_VERTEX.None, points.SegmentPoints[index]);
                }
                else
                {
                    for (int index = 0; index < points.Count; ++index)
                        this.ElementSetVertex(this.vdContext, ElementPtr, points[index], vdglTypes.FLAG_VERTEX.None, -1);
                }
                this.ElementDraw(this.vdContext, ElementPtr);
            }
            this.TestTimerEvent();
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.DrawPLine(System.Object,VectorDraw.Geometry.gPoints,System.Double)" />
        /// </summary>
        public override void DrawPLine(
          object sender,
          gPoints _points,
          double thickness,
          vdRender.PolylineFlag plineFlag)
        {
            if (this.TestDrawBreak())
                return;
            gPoints gPoints1 = _points;
            if (Globals.AreEqual(thickness, 0.0, Globals.DefaultLinearEquality) || (this.SupportLists & SupportListFlag.Create) == SupportListFlag.None && (this.ViewDir.Equals(0.0, 0.0, 1.0, Globals.DefaultVectorEquality) || this.ViewDir.Equals(0.0, 0.0, -1.0, Globals.DefaultVectorEquality)))
            {
                IntPtr ElementPtr = this.ElementInit(sender, (vdglTypes.FLAG_ELEMENT)(plineFlag & vdRender.PolylineFlag.BoundFill), -1);
                if (ElementPtr != IntPtr.Zero)
                {
                    if (gPoints1.SegmentPoints != null && gPoints1.SegmentPoints.Count == gPoints1.Count)
                    {
                        for (int index = 0; index < gPoints1.Count; ++index)
                            this.ElementSetVertex(this.vdContext, ElementPtr, gPoints1[index], vdglTypes.FLAG_VERTEX.None, gPoints1.SegmentPoints[index]);
                    }
                    else
                    {
                        for (int index = 0; index < gPoints1.Count; ++index)
                            this.ElementSetVertex(this.vdContext, ElementPtr, gPoints1[index], vdglTypes.FLAG_VERTEX.None, -1);
                    }
                    this.ElementDraw(this.vdContext, ElementPtr);
                }
            }
            else
            {
                if (!Globals.AreEqual(thickness, 0.0, Globals.DefaultLinearEquality) && (plineFlag & vdRender.PolylineFlag.BoundFill) == vdRender.PolylineFlag.BoundFill)
                {
                    this.DrawPLine(sender, _points);
                    VectorDraw.Geometry.Matrix m = new VectorDraw.Geometry.Matrix();
                    m.TranslateMatrix(0.0, 0.0, thickness);
                    this.PushMatrix(m);
                    this.DrawPLine(sender, _points);
                    this.PopMatrix();
                }
                if (gPoints1.Area() < 0.0)
                    gPoints1 = _points.Clone(false, true);
                ISupportSmoothAngle supportSmoothAngle = sender as ISupportSmoothAngle;
                double smoothAngle = this.GlobalProperties.SmoothAngle;
                if (supportSmoothAngle != null && supportSmoothAngle.SmoothAngle != -1.0)
                    smoothAngle = supportSmoothAngle.SmoothAngle;
                Vector[] smoothNormals = gPoints1.GetSmoothNormals(smoothAngle);
                if (thickness < 0.0)
                {
                    for (int index1 = 0; index1 < smoothNormals.Length; ++index1)
                    {
                        if ((gPoint)smoothNormals[index1] != (gPoint)null)
                        {
                            Vector[] vectorArray = smoothNormals;
                            int index2 = index1;
                            vectorArray[index2] = vectorArray[index2] * -1.0;
                        }
                    }
                }
                bool flag1 = smoothAngle != 0.0;
                bool flag2 = smoothAngle < 0.0;
                double num1 = 1.0;
                double num2 = 1.0;
                gPoints gPoints2 = (gPoints)null;
                vdglTypes.FLAG_ELEMENT flagElement = (vdglTypes.FLAG_ELEMENT)((vdRender.PolylineFlag)6 | plineFlag & vdRender.PolylineFlag.BoundFill);
                if (this.PenStyle.MaterialBind != null)
                {
                    flagElement |= vdglTypes.FLAG_ELEMENT.USE_TEXTURE;
                    gPoints2 = new gPoints(new gPoint[4]
                    {
            new gPoint(),
            new gPoint(),
            new gPoint(),
            new gPoint()
                    });
                    num1 = this.PenStyle.MaterialMatrix.Properties.Scales.x;
                    num2 = this.PenStyle.MaterialMatrix.Properties.Scales.y;
                }
                if (flag1)
                    flagElement |= vdglTypes.FLAG_ELEMENT.USE_NORMALS;
                vdglTypes.FLAG_ELEMENT Flag = flagElement | vdglTypes.FLAG_ELEMENT.THICK_SEGMENT;
                double num3 = 0.0;
                gPoint pt1 = new gPoint();
                gPoint pt2 = new gPoint();
                gPoint gPoint1 = new gPoint();
                gPoint gPoint2 = new gPoint();
                pt1.CopyFrom(gPoints1[0]);
                pt2.CopyFrom(gPoints1[0]);
                pt2.z += thickness;
                for (int index = 1; index < gPoints1.Count && !this.TestDrawBreak(); ++index)
                {
                    gPoint1.CopyFrom(gPoints1[index]);
                    gPoint2.CopyFrom(gPoints1[index]);
                    gPoint2.z += thickness;
                    double num4 = pt1.Distance3D(gPoint1);
                    if (gPoints2 != null)
                        gPoints2 = new gPoints(new gPoint[4]
                        {
              new gPoint(-num3, 0.0, 1.0),
              new gPoint(-(num3 + num4 * num1), 0.0, 1.0),
              new gPoint(-(num3 + num4 * num1), thickness * num2, 1.0),
              new gPoint(-num3, thickness * num2, 1.0)
                        });
                    int UserId = gPoints1.SegmentPoints == null || gPoints1.SegmentPoints.Count != gPoints1.Count ? -1 : gPoints1.SegmentPoints[index - 1];
                    IntPtr ElementPtr = this.ElementInit(sender, Flag, -1);
                    if (ElementPtr != IntPtr.Zero)
                    {
                        this.ElementSetNormal(this.vdContext, ElementPtr, smoothNormals[index - 1]);
                        if (gPoints2 != null)
                            this.ElementSetTexture(this.vdContext, ElementPtr, gPoints2[0]);
                        this.ElementSetVertex(this.vdContext, ElementPtr, pt1, vdglTypes.FLAG_VERTEX.None, UserId);
                        if (flag1)
                            this.ElementSetNormal(this.vdContext, ElementPtr, smoothNormals[index]);
                        if (gPoints2 != null)
                            this.ElementSetTexture(this.vdContext, ElementPtr, gPoints2[1]);
                        this.ElementSetVertex(this.vdContext, ElementPtr, gPoint1, flag2 ? vdglTypes.FLAG_VERTEX.INVISIBLE : vdglTypes.FLAG_VERTEX.None, UserId);
                        if (flag1)
                            this.ElementSetNormal(this.vdContext, ElementPtr, smoothNormals[index]);
                        if (gPoints2 != null)
                            this.ElementSetTexture(this.vdContext, ElementPtr, gPoints2[2]);
                        this.ElementSetVertex(this.vdContext, ElementPtr, gPoint2, vdglTypes.FLAG_VERTEX.None, UserId);
                        if (flag1)
                            this.ElementSetNormal(this.vdContext, ElementPtr, smoothNormals[index - 1]);
                        if (gPoints2 != null)
                            this.ElementSetTexture(this.vdContext, ElementPtr, gPoints2[3]);
                        this.ElementSetVertex(this.vdContext, ElementPtr, pt2, flag2 ? vdglTypes.FLAG_VERTEX.INVISIBLE : vdglTypes.FLAG_VERTEX.None, UserId);
                        this.ElementDraw(this.vdContext, ElementPtr);
                    }
                    pt1.CopyFrom(gPoint1);
                    pt2.CopyFrom(gPoint2);
                    num3 += num4;
                }
            }
            this.TestTimerEvent();
        }

        internal vdRender.DrawStatus DrawPolyfaceSectionClipCoverFaces(
          object sender,
          Int32Array FaceList,
          gPoints VertexList)
        {
            if (this.IsCreatingList)
                return this.StatusDraw;
            if (this.TestDrawBreak())
                return this.StatusDraw;
            int num1 = 0;
            if (this.SectionClips != null && this.SectionClips.Count > 0)
            {
                for (int index = 0; index < this.SectionClips.Count; ++index)
                {
                    ISectionClip sectionClip = this.SectionClips.GetItem(index);
                    if (sectionClip.Enable && (this.GlobalProperties.SectionClipCoverFaces || sectionClip.DrawSectionLines))
                        ++num1;
                }
            }
            if (num1 == 0)
                return this.StatusDraw;
            vdArray<VectorDraw.Geometry.Matrix> vdArray1 = new vdArray<VectorDraw.Geometry.Matrix>();
            vdArray<VectorDraw.Geometry.Matrix> vdArray2 = new vdArray<VectorDraw.Geometry.Matrix>();
            vdArray<linesegments> vdArray3 = new vdArray<linesegments>();
            VectorDraw.Geometry.Matrix currentMatrix = this.CurrentMatrix;
            foreach (VectorDraw.Geometry.Matrix matrix in this.sectionApplyModelMatrix.ToArray())
                currentMatrix *= matrix;
            for (int index = 0; index < this.SectionClips.Count; ++index)
            {
                ISectionClip sectionClip = this.SectionClips.GetItem(index);
                VectorDraw.Geometry.Matrix matrix = currentMatrix * this.StartedViewToWorldMatrix * sectionClip.Object2SectionMatrix;
                matrix.TranslateMatrix(0.0, 0.0, -(this.maxDepth / 5000.0));
                vdArray1.AddItem(matrix);
                vdArray2.AddItem(matrix.GetInvertion());
                vdArray3.AddItem(new linesegments());
            }
            for (int index1 = 0; index1 < FaceList.Count && !this.TestDrawBreak(); index1 += 5)
            {
                int num2 = Math.Abs(FaceList[index1]);
                gPoint vertex1 = VertexList[num2 - 1];
                int num3 = Math.Abs(FaceList[index1 + 1]);
                gPoint vertex2 = VertexList[num3 - 1];
                int num4 = Math.Abs(FaceList[index1 + 2]);
                gPoint vertex3 = VertexList[num4 - 1];
                int num5 = Math.Abs(FaceList[index1 + 3]);
                gPoint vertex4 = VertexList[num5 - 1];
                for (int index2 = 0; index2 < this.SectionClips.Count && !this.TestDrawBreak(); ++index2)
                {
                    ISectionClip sectionClip = this.SectionClips.GetItem(index2);
                    if (sectionClip.Enable && (this.GlobalProperties.SectionClipCoverFaces || sectionClip.DrawSectionLines))
                        vdArray3[index2].AddUnique(linesegments.getFaceSectionLineSegment(vdArray1[index2].Transform(vertex1), vdArray1[index2].Transform(vertex2), vdArray1[index2].Transform(vertex3), vdArray1[index2].Transform(vertex4), 1E-07), 1E-07);
                }
            }
            if (this.StatusDraw == vdRender.DrawStatus.Successed)
            {
                bool flag = !vdRender.IsColorEmpty(this.GlobalProperties.SectionClipCoverFacesColor);
                if (flag)
                    this.PushPenstyle(new vdGdiPenStyle(this.GlobalProperties.SectionClipCoverFacesColor, this.PenStyle.AlphaBlending));
                for (int index3 = 0; index3 < this.SectionClips.Count; ++index3)
                {
                    ISectionClip sectionClip = this.SectionClips.GetItem(index3);
                    if (sectionClip.Enable && (this.GlobalProperties.SectionClipCoverFaces || sectionClip.DrawSectionLines))
                    {
                        vdArray<gPoints> joinCurves = vdArray3[index3].getJoinCurves(1E-07);
                        if (joinCurves != null && joinCurves.Count != 0)
                        {
                            vdgl.DisableClipId(this.vdContext, index3, 1);
                            this.PushMatrix(vdArray2[index3]);
                            if (this.GlobalProperties.SectionClipCoverFaces)
                            {
                                vdArray<ClippingOperation> operationFlag = new vdArray<ClippingOperation>();
                                for (int index4 = 0; index4 < joinCurves.Count; ++index4)
                                    operationFlag.AddItem(ClippingOperation.XOr);
                                vdArray<gPoints> countoursTriangles = PolygonClipper.getCountoursTriangles(PolygonClipper.getCountoursPolygonObject(joinCurves, operationFlag), joinCurves);
                                if (countoursTriangles != null)
                                {
                                    foreach (gPoints inpoints in countoursTriangles)
                                        this.DrawSolidPolygon(sender, inpoints, vdRender.PolygonType.TriangleStrip);
                                }
                            }
                            if (sectionClip.DrawSectionLines)
                            {
                                foreach (gPoints points in joinCurves)
                                    this.DrawPLine(sender, points);
                            }
                            this.PopMatrix();
                            vdgl.DisableClipId(this.vdContext, index3, 0);
                            if (this.TestDrawBreak())
                                break;
                        }
                    }
                }
                if (flag)
                    this.PopPenstyle();
            }
            this.TestTimerEvent();
            return this.StatusDraw;
        }

        private bool SimpleDrawPolyface_Pattern(object sender, Int32Array FaceList, gPoints VertexList)
        {
            if (!(sender is ISupportedHatchPattern supportedHatchPattern) || supportedHatchPattern.GrHatchPattern == null)
                return false;
            if (this.TestDrawBreak())
                return true;
            if (supportedHatchPattern.GrHatchPattern.IsSolid())
            {
                vdglTypes.FLAG_ELEMENT Flag = vdglTypes.FLAG_ELEMENT.CLOSE_FIGURE | vdglTypes.FLAG_ELEMENT.FILL_ALWAYS;
                if (!this.GlobalProperties.DrawSolidHatchesOnHide)
                    Flag |= vdglTypes.FLAG_ELEMENT.HIDE_OFF;
                int count = FaceList.Count;
                for (int index = 0; index < count && !this.TestDrawBreak(); index += 5)
                {
                    int face1 = FaceList[index];
                    gPoint vertex1 = VertexList[Math.Abs(face1) - 1];
                    int face2 = FaceList[index + 1];
                    gPoint vertex2 = VertexList[Math.Abs(face2) - 1];
                    int face3 = FaceList[index + 2];
                    gPoint vertex3 = VertexList[Math.Abs(face3) - 1];
                    int face4 = FaceList[index + 3];
                    gPoint vertex4 = VertexList[Math.Abs(face4) - 1];
                    IntPtr ElementPtr = this.ElementInit((object)(sender as IRenderListItem), Flag, -1);
                    if (ElementPtr != IntPtr.Zero)
                    {
                        bool flag = Math.Abs(face4) == Math.Abs(face3) || Math.Abs(face4) == Math.Abs(face1);
                        int overlapVertex = 0;
                        int orientation = 0;
                        Vector ret = new Vector();
                        vdRender.GetOrientedNormal(vertex1, vertex2, vertex3, vertex4, ref ret, ref overlapVertex, ref orientation);
                        if (vdRenderGlobalProperties.IsFrontFaceClockWise && (double)orientation < 0.0 || !vdRenderGlobalProperties.IsFrontFaceClockWise && (double)orientation > 0.0)
                        {
                            Vector pt = ret * -1.0;
                            this.ElementSetNormal(this.vdContext, ElementPtr, pt);
                            this.ElementSetVertex(this.vdContext, ElementPtr, vertex1, vdglTypes.FLAG_VERTEX.INVISIBLE, -1);
                            if (!flag)
                                this.ElementSetVertex(this.vdContext, ElementPtr, vertex4, vdglTypes.FLAG_VERTEX.INVISIBLE, -1);
                            this.ElementSetVertex(this.vdContext, ElementPtr, vertex3, vdglTypes.FLAG_VERTEX.INVISIBLE, -1);
                            this.ElementSetVertex(this.vdContext, ElementPtr, vertex2, vdglTypes.FLAG_VERTEX.INVISIBLE, -1);
                        }
                        else
                        {
                            this.ElementSetNormal(this.vdContext, ElementPtr, ret);
                            this.ElementSetVertex(this.vdContext, ElementPtr, vertex1, vdglTypes.FLAG_VERTEX.INVISIBLE, -1);
                            this.ElementSetVertex(this.vdContext, ElementPtr, vertex2, vdglTypes.FLAG_VERTEX.INVISIBLE, -1);
                            this.ElementSetVertex(this.vdContext, ElementPtr, vertex3, vdglTypes.FLAG_VERTEX.INVISIBLE, -1);
                            if (!flag)
                                this.ElementSetVertex(this.vdContext, ElementPtr, vertex4, vdglTypes.FLAG_VERTEX.INVISIBLE, -1);
                        }
                        this.ElementDraw(this.vdContext, ElementPtr);
                    }
                }
            }
            else
            {
                IntPtr patternId = this.BindPattern(supportedHatchPattern.GrHatchPattern);
                vdglTypes.FLAG_ELEMENT Flag = (vdglTypes.FLAG_ELEMENT)(16 | 67108864);
                if (!this.GlobalProperties.DrawSolidHatchesOnHide)
                    Flag |= vdglTypes.FLAG_ELEMENT.HIDE_OFF;
                IntPtr ElementPtr = this.ElementInit((object)(sender as IRenderListItem), Flag, -1);
                if (supportedHatchPattern.HatchOrigin != (gPoint)null)
                    vdgl.ElementSetPattern(this.vdContext, ElementPtr, patternId, supportedHatchPattern.HatchOrigin.x, supportedHatchPattern.HatchOrigin.y, supportedHatchPattern.HatchOrigin.z);
                else
                    vdgl.ElementSetPattern(this.vdContext, ElementPtr, patternId, 0.0, 0.0, 0.0);
                if (ElementPtr != IntPtr.Zero)
                {
                    int count = FaceList.Count;
                    for (int index = 0; index < count && !this.TestDrawBreak(); index += 5)
                    {
                        int num1 = Math.Abs(FaceList[index]);
                        gPoint vertex5 = VertexList[num1 - 1];
                        int num2 = Math.Abs(FaceList[index + 1]);
                        gPoint vertex6 = VertexList[num2 - 1];
                        int num3 = Math.Abs(FaceList[index + 2]);
                        gPoint vertex7 = VertexList[num3 - 1];
                        int num4 = Math.Abs(FaceList[index + 3]);
                        gPoint vertex8 = VertexList[num4 - 1];
                        bool flag = Math.Abs(num4) == Math.Abs(num3) || Math.Abs(num4) == Math.Abs(num1);
                        this.ElementSetVertex(this.vdContext, ElementPtr, vertex5, vdglTypes.FLAG_VERTEX.None, -1);
                        this.ElementSetVertex(this.vdContext, ElementPtr, vertex6, vdglTypes.FLAG_VERTEX.None, -1);
                        this.ElementSetVertex(this.vdContext, ElementPtr, vertex7, vdglTypes.FLAG_VERTEX.None, -1);
                        if (!flag)
                            this.ElementSetVertex(this.vdContext, ElementPtr, vertex8, vdglTypes.FLAG_VERTEX.None, -1);
                        this.ElementSetVertex(this.vdContext, ElementPtr, vertex5, vdglTypes.FLAG_VERTEX.END_POLY, -1);
                    }
                    this.ElementDraw(this.vdContext, ElementPtr);
                }
            }
            return true;
        }

        internal virtual vdRender.DrawStatus SimpleDrawPolyface(
          object sender,
          Int32Array FaceList,
          gPoints VertexList,
          bool isMappedImagesCreateList)
        {
            if (this.SimpleDrawPolyface_Pattern(sender, FaceList, VertexList))
                return this.StatusDraw;
            gPoint uv0_1 = new gPoint(0.0, 0.0, 0.0);
            gPoint uv0_2 = new gPoint(0.0, 0.0, 0.0);
            gPoint uv0_3 = new gPoint(0.0, 0.0, 0.0);
            gPoint uv0_4 = new gPoint(0.0, 0.0, 0.0);
            VectorDraw.Geometry.Matrix _w2ecs = new VectorDraw.Geometry.Matrix();
            if (this.TestDrawBreak())
                return this.StatusDraw;
            IPolyfaceDraw polyfaceDraw = sender as IPolyfaceDraw;
            Vector[] vectorArray = (Vector[])null;
            Vector[] faceNormals = (Vector[])null;
            Int32Array OrientedFaceList = FaceList;
            ElevatedGradientColors elevatedGradientColors = (ElevatedGradientColors)null;
            DoubleArray doubleArray1 = (DoubleArray)null;
            if (polyfaceDraw != null && polyfaceDraw is IPolyface)
                doubleArray1 = ((IPolyface)polyfaceDraw).TexCoords;
            DoubleArray newtexcoords = doubleArray1;
            if (vdgl.PFaceDraw != null && polyfaceDraw != null)
            {
                double smoothAngle = polyfaceDraw.SmoothAngle;
                if (smoothAngle == -1.0)
                    smoothAngle = this.GlobalProperties.SmoothAngle;
                int num1 = vdgl.PFaceDraw(this.vdContext, vdRenderGlobalProperties.IsFrontFaceClockWise, VertexList, FaceList, doubleArray1, smoothAngle, (IElevatedColors)polyfaceDraw.GradientColors, (IgrSystemColorPalette)this.Palette) ? 1 : 0;
                int num2 = (int)this.DrawPolyfaceSectionClipCoverFaces(sender, FaceList, VertexList);
                return this.StatusDraw;
            }
            double angle = 0.0;
            if ((this.SupportLists & SupportListFlag.Create) != SupportListFlag.None || (this.SupportLists & SupportListFlag.Create) == SupportListFlag.None && this.SupportLights)
            {
                if (polyfaceDraw != null)
                {
                    angle = polyfaceDraw.SmoothAngle;
                    if (angle == -1.0)
                        angle = this.GlobalProperties.SmoothAngle;
                }
                else
                    angle = this.GlobalProperties.SmoothAngle;
                vectorArray = vdRender.GetOrientedSmoothingNormals(angle, FaceList, VertexList, doubleArray1, out OrientedFaceList, out faceNormals, out newtexcoords);
            }
            else
                OrientedFaceList = FaceList;
            if (polyfaceDraw != null && polyfaceDraw.GradientColors != null && polyfaceDraw.GradientColors.Count > 0)
                elevatedGradientColors = polyfaceDraw.GradientColors;
            if (OrientedFaceList == null)
                return vdRender.DrawStatus.Failed;
            bool flag1 = angle != 0.0;
            vdglTypes.FLAG_ELEMENT flagElement = isMappedImagesCreateList ? vdglTypes.FLAG_ELEMENT.MAPPED_IMAGE : vdglTypes.FLAG_ELEMENT.None;
            IntPtr zero = IntPtr.Zero;
            bool flag2 = !isMappedImagesCreateList && elevatedGradientColors != null;
            if (flag2)
                vdgl.PolygonModeOverWrite(this.vdContext, 2);
            Vector pt1 = (Vector)null;
            Vector pt2 = (Vector)null;
            Vector pt3 = (Vector)null;
            Vector pt4 = (Vector)null;
            Vector vector = (Vector)null;
            int num3 = -1;
            vdGdiPenStyle newstyle = (vdGdiPenStyle)null;
            int count = this.PenStyleStack.Count;
            this.mPenStyleDisableWrite = true;
            bool flag3 = false;
            for (int index1 = 0; index1 < OrientedFaceList.Count && !this.TestDrawBreak(); index1 += 5)
            {
                int UserId = index1 / 5;
                int index2 = 4 * UserId;
                int num4 = OrientedFaceList[index1];
                gPoint vertex1 = VertexList[Math.Abs(num4) - 1];
                int num5 = OrientedFaceList[index1 + 1];
                gPoint vertex2 = VertexList[Math.Abs(num5) - 1];
                int num6 = OrientedFaceList[index1 + 2];
                gPoint vertex3 = VertexList[Math.Abs(num6) - 1];
                int num7 = OrientedFaceList[index1 + 3];
                gPoint vertex4 = VertexList[Math.Abs(num7) - 1];
                if (vectorArray != null)
                {
                    pt1 = vectorArray[index2];
                    pt2 = vectorArray[index2 + 1];
                    pt3 = vectorArray[index2 + 2];
                    pt4 = vectorArray[index2 + 3];
                }
                int index3 = OrientedFaceList[index1 + 4];
                if (!isMappedImagesCreateList && num3 != index3)
                {
                    num3 = index3;
                    if (newstyle != (vdGdiPenStyle)null)
                    {
                        this.PopPenstyle();
                        newstyle = (vdGdiPenStyle)null;
                        this.SetActivePenStyle(false, vdrawglRender.PenstyleFlag.None);
                        flag3 = true;
                    }
                    if (this.Palette != null && index3 >= 0 && newstyle == (vdGdiPenStyle)null)
                    {
                        newstyle = this.Palette[index3];
                        this.PushPenstyle(newstyle);
                        this.SetActivePenStyle(false, vdrawglRender.PenstyleFlag.None);
                        flag3 = true;
                    }
                }
                vdGdiPenStyle penStyle = this.PenStyle;
                vdglTypes.FLAG_ELEMENT Flag = flagElement | vdglTypes.FLAG_ELEMENT.CLOSE_FIGURE | vdglTypes.FLAG_ELEMENT.FILL_ON | (flag1 ? vdglTypes.FLAG_ELEMENT.USE_NORMALS : vdglTypes.FLAG_ELEMENT.None) | (isMappedImagesCreateList || penStyle.MaterialBind == null || !(penStyle.MaterialMatrix != (VectorDraw.Geometry.Matrix)null) ? vdglTypes.FLAG_ELEMENT.None : vdglTypes.FLAG_ELEMENT.USE_TEXTURE);
                if (flag2)
                    Flag |= vdglTypes.FLAG_ELEMENT.USE_COLORS;
                if (faceNormals != null)
                    vector = faceNormals[UserId];
                if ((gPoint)vector != (gPoint)null && (Flag & vdglTypes.FLAG_ELEMENT.USE_TEXTURE) == vdglTypes.FLAG_ELEMENT.USE_TEXTURE)
                {
                    if (newtexcoords != null)
                    {
                        int num8 = UserId * 4 * 2;
                        gPoint gPoint1 = uv0_1;
                        DoubleArray doubleArray2 = newtexcoords;
                        int index4 = num8;
                        int num9 = index4 + 1;
                        double num10 = doubleArray2[index4];
                        gPoint1.x = num10;
                        gPoint gPoint2 = uv0_1;
                        DoubleArray doubleArray3 = newtexcoords;
                        int index5 = num9;
                        int num11 = index5 + 1;
                        double num12 = doubleArray3[index5];
                        gPoint2.y = num12;
                        uv0_1.z = 1.0;
                        gPoint gPoint3 = uv0_2;
                        DoubleArray doubleArray4 = newtexcoords;
                        int index6 = num11;
                        int num13 = index6 + 1;
                        double num14 = doubleArray4[index6];
                        gPoint3.x = num14;
                        gPoint gPoint4 = uv0_2;
                        DoubleArray doubleArray5 = newtexcoords;
                        int index7 = num13;
                        int num15 = index7 + 1;
                        double num16 = doubleArray5[index7];
                        gPoint4.y = num16;
                        uv0_2.z = 1.0;
                        gPoint gPoint5 = uv0_3;
                        DoubleArray doubleArray6 = newtexcoords;
                        int index8 = num15;
                        int num17 = index8 + 1;
                        double num18 = doubleArray6[index8];
                        gPoint5.x = num18;
                        gPoint gPoint6 = uv0_3;
                        DoubleArray doubleArray7 = newtexcoords;
                        int index9 = num17;
                        int num19 = index9 + 1;
                        double num20 = doubleArray7[index9];
                        gPoint6.y = num20;
                        uv0_3.z = 1.0;
                        gPoint gPoint7 = uv0_4;
                        DoubleArray doubleArray8 = newtexcoords;
                        int index10 = num19;
                        int num21 = index10 + 1;
                        double num22 = doubleArray8[index10];
                        gPoint7.x = num22;
                        gPoint gPoint8 = uv0_4;
                        DoubleArray doubleArray9 = newtexcoords;
                        int index11 = num21;
                        int num23 = index11 + 1;
                        double num24 = doubleArray9[index11];
                        gPoint8.y = num24;
                        uv0_4.z = 1.0;
                    }
                    else
                    {
                        _w2ecs.IdentityMatrix();
                        _w2ecs.ApplyWCS2ECS(vector);
                        vdrawglRender.TransformUV(_w2ecs, penStyle.MaterialMatrix, vertex1, ref uv0_1);
                        vdrawglRender.TransformUV(_w2ecs, penStyle.MaterialMatrix, vertex2, ref uv0_2);
                        vdrawglRender.TransformUV(_w2ecs, penStyle.MaterialMatrix, vertex3, ref uv0_3);
                        vdrawglRender.TransformUV(_w2ecs, penStyle.MaterialMatrix, vertex4, ref uv0_4);
                    }
                }
                bool flag4 = Math.Abs(num7) == Math.Abs(num6) || Math.Abs(num7) == Math.Abs(num4);
                IntPtr ElementPtr = this.ElementInit(sender, Flag, UserId);
                if (!(ElementPtr == IntPtr.Zero))
                {
                    if ((gPoint)vector != (gPoint)null)
                        this.ElementSetNormal(this.vdContext, ElementPtr, vector);
                    if (flag1)
                        this.ElementSetNormal(this.vdContext, ElementPtr, pt1);
                    if ((Flag & vdglTypes.FLAG_ELEMENT.USE_TEXTURE) != vdglTypes.FLAG_ELEMENT.None)
                        this.ElementSetTexture(this.vdContext, ElementPtr, uv0_1);
                    if (flag2)
                        this.ElementSetColorEx(this.vdContext, ElementPtr, elevatedGradientColors.GetAt(vertex1.z, Color.Empty));
                    this.ElementSetVertex(this.vdContext, ElementPtr, vertex1, num4 < 0 ? vdglTypes.FLAG_VERTEX.INVISIBLE : vdglTypes.FLAG_VERTEX.None, -1);
                    if (flag1)
                        this.ElementSetNormal(this.vdContext, ElementPtr, pt2);
                    if ((Flag & vdglTypes.FLAG_ELEMENT.USE_TEXTURE) != vdglTypes.FLAG_ELEMENT.None)
                        this.ElementSetTexture(this.vdContext, ElementPtr, uv0_2);
                    if (flag2)
                        this.ElementSetColorEx(this.vdContext, ElementPtr, elevatedGradientColors.GetAt(vertex2.z, Color.Empty));
                    this.ElementSetVertex(this.vdContext, ElementPtr, vertex2, num5 < 0 ? vdglTypes.FLAG_VERTEX.INVISIBLE : vdglTypes.FLAG_VERTEX.None, -1);
                    if (flag1)
                        this.ElementSetNormal(this.vdContext, ElementPtr, pt3);
                    if ((Flag & vdglTypes.FLAG_ELEMENT.USE_TEXTURE) != vdglTypes.FLAG_ELEMENT.None)
                        this.ElementSetTexture(this.vdContext, ElementPtr, uv0_3);
                    if (flag2)
                        this.ElementSetColorEx(this.vdContext, ElementPtr, elevatedGradientColors.GetAt(vertex3.z, Color.Empty));
                    this.ElementSetVertex(this.vdContext, ElementPtr, vertex3, num6 < 0 ? vdglTypes.FLAG_VERTEX.INVISIBLE : vdglTypes.FLAG_VERTEX.None, -1);
                    if (!flag4)
                    {
                        if (flag1)
                            this.ElementSetNormal(this.vdContext, ElementPtr, pt4);
                        if ((Flag & vdglTypes.FLAG_ELEMENT.USE_TEXTURE) != vdglTypes.FLAG_ELEMENT.None)
                            this.ElementSetTexture(this.vdContext, ElementPtr, uv0_4);
                        if (flag2)
                            this.ElementSetColorEx(this.vdContext, ElementPtr, elevatedGradientColors.GetAt(vertex4.z, Color.Empty));
                        this.ElementSetVertex(this.vdContext, ElementPtr, vertex4, num7 < 0 ? vdglTypes.FLAG_VERTEX.INVISIBLE : vdglTypes.FLAG_VERTEX.None, -1);
                    }
                    this.ElementDraw(this.vdContext, ElementPtr);
                    this.TestTimerEvent();
                }
                else
                    break;
            }
            while (this.PenStyleStack.Count > count)
                this.PopPenstyle();
            this.mPenStyleDisableWrite = false;
            if (flag3)
                this.PenStyleChanged((vdGdiPenStyle)null);
            if (flag2)
                vdgl.PolygonModeOverWrite(this.vdContext, -1);
            this.TestTimerEvent();
            int num25 = (int)this.DrawPolyfaceSectionClipCoverFaces(sender, FaceList, VertexList);
            return this.StatusDraw;
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.DrawPolyface(System.Object,VectorDraw.Geometry.Int32Array,VectorDraw.Geometry.gPoints)" />
        /// </summary>
        public override vdRender.DrawStatus DrawPolyface(
          object sender,
          Int32Array FaceList,
          gPoints VertexList)
        {
            if (this.TestDrawBreak())
                return this.StatusDraw;
            int num = 0;
            if (sender is ISupportdMappedImages)
                num = (sender as ISupportdMappedImages).GetNumMappedImages();
            return this.SimpleDrawPolyface(sender, FaceList, VertexList, num > 0);
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.DrawFace4(System.Object,VectorDraw.Geometry.gPoint,VectorDraw.Geometry.gPoint,VectorDraw.Geometry.gPoint,VectorDraw.Geometry.gPoint,System.Boolean,System.Boolean,System.Boolean,System.Boolean)" />
        /// </summary>
        public override void DrawFace4(
          object sender,
          gPoint p1,
          gPoint p2,
          gPoint p3,
          gPoint p4,
          bool visible1,
          bool visible2,
          bool visible3,
          bool visible4)
        {
            gPoint uv0 = new gPoint(0.0, 0.0, 0.0);
            gPoint uv1 = new gPoint(0.0, 0.0, 0.0);
            gPoint uv2 = new gPoint(0.0, 0.0, 0.0);
            gPoint uv3 = new gPoint(0.0, 0.0, 0.0);
            Vector normal = new Vector();
            if (this.TestDrawBreak())
                return;
            vdglTypes.FLAG_ELEMENT Flag = vdglTypes.FLAG_ELEMENT.CLOSE_FIGURE | vdglTypes.FLAG_ELEMENT.FILL_ON;
            VectorDraw.Geometry.Matrix tmat = (VectorDraw.Geometry.Matrix)null;
            if (this.PenStyle.MaterialBind != null)
                tmat = this.PenStyle.MaterialMatrix;
            if (vdrawglRender.createEcsToWorldUVS(ref p1, ref p2, ref p3, ref p4, ref visible1, ref visible2, ref visible3, ref visible4, tmat, ref normal, ref uv0, ref uv1, ref uv2, ref uv3))
                Flag |= tmat != (VectorDraw.Geometry.Matrix)null ? vdglTypes.FLAG_ELEMENT.USE_TEXTURE : vdglTypes.FLAG_ELEMENT.None;
            IntPtr ElementPtr = this.ElementInit(sender, Flag, -1);
            if (ElementPtr != IntPtr.Zero)
            {
                this.ElementSetNormal(this.vdContext, ElementPtr, normal);
                if ((Flag & vdglTypes.FLAG_ELEMENT.USE_TEXTURE) != vdglTypes.FLAG_ELEMENT.None)
                    this.ElementSetTexture(this.vdContext, ElementPtr, uv0);
                this.ElementSetVertex(this.vdContext, ElementPtr, p1, visible1 ? vdglTypes.FLAG_VERTEX.None : vdglTypes.FLAG_VERTEX.INVISIBLE, -1);
                if ((Flag & vdglTypes.FLAG_ELEMENT.USE_TEXTURE) != vdglTypes.FLAG_ELEMENT.None)
                    this.ElementSetTexture(this.vdContext, ElementPtr, uv1);
                this.ElementSetVertex(this.vdContext, ElementPtr, p2, visible2 ? vdglTypes.FLAG_VERTEX.None : vdglTypes.FLAG_VERTEX.INVISIBLE, -1);
                if ((Flag & vdglTypes.FLAG_ELEMENT.USE_TEXTURE) != vdglTypes.FLAG_ELEMENT.None)
                    this.ElementSetTexture(this.vdContext, ElementPtr, uv2);
                this.ElementSetVertex(this.vdContext, ElementPtr, p3, visible3 ? vdglTypes.FLAG_VERTEX.None : vdglTypes.FLAG_VERTEX.INVISIBLE, -1);
                if ((Flag & vdglTypes.FLAG_ELEMENT.USE_TEXTURE) != vdglTypes.FLAG_ELEMENT.None)
                    this.ElementSetTexture(this.vdContext, ElementPtr, uv3);
                this.ElementSetVertex(this.vdContext, ElementPtr, p4, visible4 ? vdglTypes.FLAG_VERTEX.None : vdglTypes.FLAG_VERTEX.INVISIBLE, -1);
                this.ElementDraw(this.vdContext, ElementPtr);
            }
            this.TestTimerEvent();
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.DrawPolygon(System.Object,VectorDraw.Geometry.gPoints,VectorDraw.Geometry.Vector,System.Boolean)" />
        /// </summary>
        public override void DrawPolygon(
          object sender,
          gPoints points,
          Vector PlaneNormal,
          bool FillOn)
        {
            this.DrawPolygon_Vector = PlaneNormal;
            if ((gPoint)this.DrawPolygon_Vector == (gPoint)null)
                this.DrawPolygon_Vector = points.GetNormal();
            base.DrawPolygon(sender, points, PlaneNormal, FillOn);
            this.DrawPolygon_Vector = (Vector)null;
        }

        internal virtual void Pure_DrawSolidPolygon(
          object sender,
          ImageBind materilaBind,
          VectorDraw.Geometry.Matrix materialMatrix,
          gPoints ptfs,
          vdRender.PolygonType ptype,
          vdglTypes.FLAG_ELEMENT eflag)
        {
            Vector N = new Vector(0.0, 0.0, 1.0);
            if ((gPoint)this.DrawPolygon_Vector != (gPoint)null)
                N = new Vector(this.DrawPolygon_Vector);
            if (!(this is ActionWrapperRender) && !(this is RenderSelect) && (eflag & vdglTypes.FLAG_ELEMENT.FROM_TRIANGL_STRIP) == vdglTypes.FLAG_ELEMENT.None)
            {
                if (ptfs.Count <= 5 && ptfs.IsClosed())
                {
                    double num = ptfs.Area3D(N);
                    ptfs = ptfs.Clone(false, num > 0.0);
                    ptfs.RemoveLast();
                    eflag |= vdglTypes.FLAG_ELEMENT.CLOSE_FIGURE | vdglTypes.FLAG_ELEMENT.FROM_TRIANGL_STRIP;
                    ptype = vdRender.PolygonType.Simple_CCW;
                }
                else
                {
                    double num = ptfs.Area3D(N);
                    ptfs = ptfs.Clone(false, num > 0.0);
                    ptype = vdRender.PolygonType.Simple_CCW;
                    vdArray<gPoints> inputCountours = new vdArray<gPoints>(new gPoints[1]
                    {
            ptfs
                    });
                    vdArray<gPoints> countours = PolygonClipper.getCountours(PolygonClipper.getCountoursPolygonObject(inputCountours, new vdArray<ClippingOperation>(new ClippingOperation[1]
                    {
            ClippingOperation.XOr
                    })), inputCountours);
                    if (countours == null)
                        return;
                    IEnumerator enumerator = countours.GetEnumerator();
                    try
                    {
                        while (enumerator.MoveNext())
                        {
                            gPoints current = (gPoints)enumerator.Current;
                            this.Pure_DrawSolidPolygon(sender, materilaBind, materialMatrix, current, ptype, (eflag | vdglTypes.FLAG_ELEMENT.FROM_TRIANGL_STRIP | vdglTypes.FLAG_ELEMENT.FILL_ALWAYS | vdglTypes.FLAG_ELEMENT.FILL_ON) ^ vdglTypes.FLAG_ELEMENT.FILL_ON);
                        }
                        return;
                    }
                    finally
                    {
                        if (enumerator is IDisposable disposable)
                            disposable.Dispose();
                    }
                }
            }
            if (materilaBind == null)
                materilaBind = this.PenStyle.MaterialBind;
            if (materialMatrix == (VectorDraw.Geometry.Matrix)null)
                materialMatrix = this.PenStyle.MaterialMatrix;
            IntPtr zero = IntPtr.Zero;
            double num1;
            switch (ptype)
            {
                case vdRender.PolygonType.Simple_CCW:
                case vdRender.PolygonType.Simple_CCW_SUPPORT_TRISTRIP:
                    num1 = -1.0;
                    break;
                case vdRender.PolygonType.Simple_CW:
                    num1 = 1.0;
                    break;
                default:
                    num1 = !((gPoint)this.DrawPolygon_Vector != (gPoint)null) ? ptfs.Area() : ptfs.Area3D(this.DrawPolygon_Vector) * -1.0;
                    break;
            }
            Vector vector = N * (num1 > 0.0 ? 1.0 : -1.0);
            eflag |= materilaBind == null || !(materialMatrix != (VectorDraw.Geometry.Matrix)null) ? vdglTypes.FLAG_ELEMENT.None : vdglTypes.FLAG_ELEMENT.USE_TEXTURE;
            IntPtr ElementPtr = this.ElementInit(sender, eflag, -1);
            if (!(ElementPtr != IntPtr.Zero))
                return;
            this.ElementSetNormal(this.vdContext, ElementPtr, vector);
            gPoints gPoints = (gPoints)null;
            if ((eflag & vdglTypes.FLAG_ELEMENT.USE_TEXTURE) != vdglTypes.FLAG_ELEMENT.None)
            {
                VectorDraw.Geometry.Matrix _w2ecs = new VectorDraw.Geometry.Matrix();
                _w2ecs.ApplyWCS2ECS(vector);
                VectorDraw.Geometry.Matrix tmat = materialMatrix;
                gPoints = new gPoints();
                for (int index = 0; index < ptfs.Count; ++index)
                {
                    gPoint uv0 = new gPoint();
                    vdrawglRender.TransformUV(_w2ecs, tmat, ptfs[index], ref uv0);
                    gPoints.Add(uv0);
                }
            }
            for (int index = 0; index < ptfs.Count; ++index)
            {
                if (gPoints != null)
                    this.ElementSetTexture(this.vdContext, ElementPtr, gPoints[index]);
                this.ElementSetVertex(this.vdContext, ElementPtr, ptfs[index], vdglTypes.FLAG_VERTEX.INVISIBLE, -1);
            }
            this.ElementDraw(this.vdContext, ElementPtr);
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.DrawSolidPolygon(System.Object,VectorDraw.Geometry.gPoints,VectorDraw.Render.vdRender.PolygonType)" />
        /// </summary>
        public override void DrawSolidPolygon(
          object sender,
          gPoints inpoints,
          vdRender.PolygonType ptype)
        {
            if (this.TestDrawBreak())
                return;
            vdglTypes.FLAG_ELEMENT eflag = (vdglTypes.FLAG_ELEMENT)(18 | (this.PenStyle.MaterialBind != null ? 1 : 0));
            gPoints gPoints = inpoints;
            switch (ptype)
            {
                case vdRender.PolygonType.TriangleStrip:
                    eflag |= vdglTypes.FLAG_ELEMENT.FROM_TRIANGL_STRIP;
                    gPoints = gPoints.gPointsFromTriangleStrip(inpoints);
                    break;
                case vdRender.PolygonType.Simple_CCW_SUPPORT_TRISTRIP:
                    eflag |= vdglTypes.FLAG_ELEMENT.FROM_TRIANGL_STRIP;
                    break;
            }
            if (this.PenStyle.MaterialBind == null && this.PenStyle.PrintingExtra != null && this.PenStyle.PrintingExtra.FillStyle != PSFillStyleFlag.UseObject && this.PenStyle.PrintingExtra.FillStyle != PSFillStyleFlag.Solid)
            {
                int num = (int)grHatch.FillPolygon(sender, (vdRender)this, this.PenStyle.PrintingExtra.FillStyle, gPoints);
                this.TestTimerEvent();
            }
            else
            {
                this.Pure_DrawSolidPolygon(sender, (ImageBind)null, (VectorDraw.Geometry.Matrix)null, gPoints, ptype, eflag);
                this.TestTimerEvent();
            }
        }

        internal IntPtr BindLineType(LineType linetype) => linetype == null ? IntPtr.Zero : linetype.GetBindPtr((vdRender)this, this.vdContext);

        internal IntPtr BindPattern(grPattern pattern) => pattern == null ? IntPtr.Zero : pattern.GetBindPtr(this.vdContext);

        internal virtual void DrawHatchedPolyPolygon(
          object sender,
          vdArray<gPoints> inpoints,
          grPattern pattern,
          gPoint origin,
          vdglTypes.FLAG_ELEMENT eflag,
          Vector _normal)
        {
            IntPtr patternId = this.BindPattern(pattern);
            if ((gPoint)_normal == (gPoint)null)
                _normal = new Vector(0.0, 0.0, 1.0);
            eflag |= vdglTypes.FLAG_ELEMENT.None;
            IntPtr zero = IntPtr.Zero;
            IntPtr ElementPtr = this.ElementInit(sender, eflag, -1);
            if (ElementPtr != IntPtr.Zero)
            {
                if (origin != (gPoint)null)
                    vdgl.ElementSetPattern(this.vdContext, ElementPtr, patternId, origin.x, origin.y, origin.z);
                else
                    vdgl.ElementSetPattern(this.vdContext, ElementPtr, patternId, 0.0, 0.0, 0.0);
                this.ElementSetNormal(this.vdContext, ElementPtr, _normal);
                for (int index1 = 0; index1 < inpoints.Count; ++index1)
                {
                    bool flag = inpoints[index1].SegmentPoints != null && inpoints[index1].SegmentPoints.Count == inpoints[index1].Count;
                    for (int index2 = 0; index2 < inpoints[index1].Count; ++index2)
                    {
                        int UserId = -1;
                        if (flag)
                            UserId = inpoints[index1].SegmentPoints[index2];
                        this.ElementSetVertex(this.vdContext, ElementPtr, inpoints[index1][index2], (vdglTypes.FLAG_VERTEX)(2 | (index2 == inpoints[index1].Count - 1 ? 4 : 0)), UserId);
                    }
                }
                this.ElementDraw(this.vdContext, ElementPtr);
            }
            this.TestTimerEvent();
        }

        internal virtual void DrawHatchedPolyPolygon(
          object sender,
          vdArray<gPoints> inpoints,
          grPattern pattern,
          gPoint origin,
          vdglTypes.FLAG_ELEMENT eflag,
          bool forcePolygonClip,
          Vector normal)
        {
            if (this.TestDrawBreak() || inpoints.Count == 0)
                return;
            if ((gPoint)normal == (gPoint)null)
                normal = new Vector(0.0, 0.0, 1.0);
            if (forcePolygonClip)
                forcePolygonClip = grHatch.TestToTalItemsClip(inpoints);
            if (!(this is ActionWrapperRender) && !(this is RenderSelect) && (eflag & vdglTypes.FLAG_ELEMENT.FROM_TRIANGL_STRIP) == vdglTypes.FLAG_ELEMENT.None)
            {
                if (inpoints.Count == 1 && inpoints[0].Count <= 5 && inpoints[0].IsClosed())
                {
                    gPoints gPoints = inpoints[0].Clone(false, inpoints[0].Area() > 0.0);
                    gPoints.RemoveLast();
                    inpoints = new vdArray<gPoints>();
                    inpoints.AddItem(gPoints);
                    eflag |= vdglTypes.FLAG_ELEMENT.CLOSE_FIGURE | vdglTypes.FLAG_ELEMENT.FROM_TRIANGL_STRIP;
                    normal = new Vector(0.0, 0.0, -1.0);
                }
                else if (forcePolygonClip)
                {
                    int num = -1;
                    if (inpoints.Count > 0 && inpoints[0].SegmentPoints != null && inpoints[0].SegmentPoints.Count > 0)
                        num = inpoints[0].SegmentPoints[0];
                    vdArray<ClippingOperation> operationFlag = new vdArray<ClippingOperation>();
                    vdArray<gPoints> inputCountours = new vdArray<gPoints>();
                    for (int index = 0; index < inpoints.Count; ++index)
                    {
                        operationFlag.AddItem(ClippingOperation.XOr);
                        gPoints gPoints = inpoints[index].Clone(false, inpoints[index].Area() > 0.0);
                        inputCountours.AddItem(gPoints);
                    }
                    normal = new Vector(0.0, 0.0, -1.0);
                    vdArray<gPoints> countours = PolygonClipper.getCountours(PolygonClipper.getCountoursPolygonObject(inputCountours, operationFlag), inputCountours);
                    if (countours == null)
                        return;
                    IEnumerator enumerator = countours.GetEnumerator();
                    try
                    {
                        while (enumerator.MoveNext())
                        {
                            gPoints current = (gPoints)enumerator.Current;
                            if (num >= 0)
                            {
                                current.SegmentPoints = new Int32Array();
                                for (int index = 0; index < current.Count; ++index)
                                    current.SegmentPoints.Add((object)inpoints[0].SegmentPoints[0]);
                            }
                            this.DrawHatchedPolyPolygon(sender, new vdArray<gPoints>(new gPoints[1]
                            {
                current
                            }), pattern, origin, (eflag | vdglTypes.FLAG_ELEMENT.FROM_TRIANGL_STRIP | vdglTypes.FLAG_ELEMENT.FILL_ALWAYS | vdglTypes.FLAG_ELEMENT.FILL_ON) ^ vdglTypes.FLAG_ELEMENT.FILL_ON, false, normal);
                        }
                        return;
                    }
                    finally
                    {
                        if (enumerator is IDisposable disposable)
                            disposable.Dispose();
                    }
                }
            }
            this.DrawHatchedPolyPolygon(sender, inpoints, pattern, origin, eflag, normal);
        }

        /// <summary>
        /// Fills an array of closed regions with the passed pattern
        /// </summary>
        /// <param name="sender">The object that call this method.</param>
        /// <param name="inpoints">An array of closed regions in World coord system</param>
        /// <param name="pattern">A that will fill the passed regions.</param>
        /// <param name="origin">A point in World coord system that the pattern will begin the filling.</param>
        public void DrawHatchedPolyPolygon(
          object sender,
          vdArray<gPoints> inpoints,
          grPattern pattern,
          gPoint origin)
        {
            this.DrawHatchedPolyPolygon(sender, inpoints, pattern, origin, true);
        }

        /// <summary>
        /// Fills an array of closed regions with the passed pattern
        /// </summary>
        /// <param name="sender">The object that call this method.</param>
        /// <param name="inpoints">An array of closed regions in World coord system</param>
        /// <param name="pattern">A that will fill the passed regions.</param>
        /// <param name="origin">A point in World coord system that the pattern will begin the filling.</param>
        /// <param name="Wire2dSolidFill">True in order to be filled on wire render mode and when pattern is null or solid.</param>
        public void DrawHatchedPolyPolygon(
          object sender,
          vdArray<gPoints> inpoints,
          grPattern pattern,
          gPoint origin,
          bool Wire2dSolidFill)
        {
            vdglTypes.FLAG_ELEMENT flagElement = vdglTypes.FLAG_ELEMENT.CLOSE_FIGURE | vdglTypes.FLAG_ELEMENT.COMPLEX_POLY;
            vdglTypes.FLAG_ELEMENT eflag = !Wire2dSolidFill ? flagElement | vdglTypes.FLAG_ELEMENT.FILL_ON : flagElement | vdglTypes.FLAG_ELEMENT.FILL_ALWAYS;
            if (pattern != null)
                eflag |= pattern.GL_ListElementFlag;
            this.DrawHatchedPolyPolygon(sender, inpoints, pattern, origin, eflag, true, (Vector)null);
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.DrawImagePolygon(System.Object,VectorDraw.Render.ImageBind,System.Double,VectorDraw.Geometry.Vector,VectorDraw.Geometry.gPoints)" />
        /// </summary>
        public override void DrawImagePolygon(
          object sender,
          ImageBind cImage,
          double angle,
          Vector scale,
          gPoints inpoints)
        {
            if (this.TestDrawBreak())
                return;
            VectorDraw.Geometry.Matrix matrix = new VectorDraw.Geometry.Matrix();
            matrix.ScaleMatrix(-((double)cImage.Width * scale.x), (double)cImage.Height * scale.y, scale.z);
            matrix.RotateZMatrix(angle);
            matrix.Invert();
            IntPtr ImageId = this.BindImage(cImage);
            vdgl.SetMaterial(this.vdContext, (int)byte.MaxValue, (int)byte.MaxValue, (int)byte.MaxValue, (int)this.PenStyle.AlphaBlending, vdglTypes.PenWidthFlag.PIXEL, 0.0, IntPtr.Zero, 0.0, ImageId, vdgl.AsvdrawContextMatrix(matrix), 0, 0, 0, 0, vdglTypes.MATERIAL_FLAG.PUSHED | vdglTypes.MATERIAL_FLAG.IGNORE_LOCK);
            this.Pure_DrawSolidPolygon(sender, cImage, matrix, inpoints, vdRenderGlobalProperties.IsFrontFaceClockWise ? vdRender.PolygonType.Simple_CW : vdRender.PolygonType.Simple_CCW, vdglTypes.FLAG_ELEMENT.USE_TEXTURE | vdglTypes.FLAG_ELEMENT.CLOSE_FIGURE | vdglTypes.FLAG_ELEMENT.IMAGE | vdglTypes.FLAG_ELEMENT.FILL_ALWAYS);
            this.SetActivePenStyle(false, vdrawglRender.PenstyleFlag.poped | vdrawglRender.PenstyleFlag.ignoreLock);
            this.TestTimerEvent();
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.DrawImage(System.Object,VectorDraw.Render.ImageBind,System.Double,System.Double)" />
        /// </summary>
        public override void DrawImage(object sender, ImageBind cImage, double cWidth, double cHeight)
        {
            if (this.TestDrawBreak())
                return;
            if (this.IsWire2d && !(this is ActionWrapperRender) && this.graphics != null && !this.IsCreatingList && cImage.IsEMF && (this.GlobalProperties.Wire2dSectionClip == vdRenderGlobalProperties.Wire2dSectionClipFlag.Off || this.SectionClips == null || this.SectionClips.Count == 0) && (this.ViewDir.Equals(0.0, 0.0, 1.0, Globals.DefaultVectorEquality) || this.ViewDir.Equals(0.0, 0.0, -1.0, Globals.DefaultVectorEquality)))
            {
                this.mIsQuickLock = true;
                bool isLock = this.IsLock;
                if (isLock)
                    this.UnLock();
                Vector scale = new Vector(cWidth / (double)cImage.Width, cHeight / (double)cImage.Height, 1.0);
                double angle = 0.0;
                GDIPlusRender.GdiDrawImage(this.graphics, cImage, angle, scale, this.World2Pixelmatrix);
                if (isLock)
                    this.Lock();
                this.mIsQuickLock = false;
            }
            else
            {
                VectorDraw.Geometry.Matrix mat = new VectorDraw.Geometry.Matrix();
                mat.ScaleMatrix((double)cImage.Width, (double)cImage.Height, 1.0);
                vdgl.SetMaterial(this.vdContext, (int)byte.MaxValue, (int)byte.MaxValue, (int)byte.MaxValue, (int)this.PenStyle.AlphaBlending, vdglTypes.PenWidthFlag.PIXEL, 0.0, IntPtr.Zero, 0.0, this.BindImage(cImage), vdgl.AsvdrawContextMatrix(mat), 0, 0, 0, 0, vdglTypes.MATERIAL_FLAG.PUSHED | vdglTypes.MATERIAL_FLAG.IGNORE_LOCK);
                vdglTypes.FLAG_ELEMENT Flag = vdglTypes.FLAG_ELEMENT.USE_TEXTURE | vdglTypes.FLAG_ELEMENT.CLOSE_FIGURE | vdglTypes.FLAG_ELEMENT.IMAGE | vdglTypes.FLAG_ELEMENT.FILL_ALWAYS;
                IntPtr ElementPtr = this.ElementInit(sender, Flag, -1);
                if (ElementPtr != IntPtr.Zero)
                {
                    this.ElementSetNormal(this.vdContext, ElementPtr, new Vector(0.0, 0.0, -1.0));
                    this.ElementSetTexture(this.vdContext, ElementPtr, new gPoint(0.0, 0.0, 1.0));
                    this.ElementSetVertex(this.vdContext, ElementPtr, new gPoint(0.0, 0.0, 0.0), vdglTypes.FLAG_VERTEX.None, -1);
                    this.ElementSetTexture(this.vdContext, ElementPtr, new gPoint(1.0, 0.0, 1.0));
                    this.ElementSetVertex(this.vdContext, ElementPtr, new gPoint(cWidth, 0.0, 0.0), vdglTypes.FLAG_VERTEX.None, -1);
                    this.ElementSetTexture(this.vdContext, ElementPtr, new gPoint(1.0, 1.0, 1.0));
                    this.ElementSetVertex(this.vdContext, ElementPtr, new gPoint(cWidth, cHeight, 0.0), vdglTypes.FLAG_VERTEX.None, -1);
                    this.ElementSetTexture(this.vdContext, ElementPtr, new gPoint(0.0, 1.0, 1.0));
                    this.ElementSetVertex(this.vdContext, ElementPtr, new gPoint(0.0, cHeight, 0.0), vdglTypes.FLAG_VERTEX.None, -1);
                    this.ElementDraw(this.vdContext, ElementPtr);
                }
                this.SetActivePenStyle(false, vdrawglRender.PenstyleFlag.poped | vdrawglRender.PenstyleFlag.ignoreLock);
            }
            this.TestTimerEvent();
        }

        internal virtual unsafe void OnDrawElement_String(
          IntPtr chars,
          int nchars,
          int flag,
          IntPtr FontName,
          int FontNameLength,
          float FontSize,
          int fontStyle,
          byte[] colorRGBA,
          float thickness,
          float[] box2d,
          double[] modelmatrix,
          double[] world2pixel,
          ref int Cancel)
        {
            Box box;
            if ((flag & 1) == 0)
                return;
            VectorDraw.Geometry.Matrix matrix1 = vdgl.ReadMatrix(world2pixel);
            try
            {
                 box = new Box(new gPoint((double)box2d[0], (double)box2d[1]), new gPoint((double)box2d[2], (double)box2d[3]));
            }
            catch
            {
                return;
            }
            bool flag1 = false;
            if (!this.IsPrinting)
            {
                Vector v = new Vector(0.0, (double)box2d[3] - (double)box2d[1], 0.0);
                matrix1.TransformVector(v, false);
                if (v.Length < vdRenderGlobalProperties.MinTextHeight * (double)this.DpiY)
                {
                    this.DrawSolidBoundBox((object)this, box);
                    flag1 = true;
                    Cancel |= 2;
                }
            }
            if (!flag1)
            {
                if ((this.GlobalProperties.Wire2dUseFontOutLines & vdRenderGlobalProperties.Wire2dUseFontOutLinesFlag.UseNativeTTFLists) != vdRenderGlobalProperties.Wire2dUseFontOutLinesFlag.Default)
                    return;
                VectorDraw.Geometry.Matrix matrix2 = vdgl.ReadMatrix(modelmatrix);
                Vector zdir = matrix2.Zdir;
                if (!Globals.AreEqual(zdir.x, 0.0, Globals.DefaultVectorEquality) || !Globals.AreEqual(zdir.y, 0.0, Globals.DefaultVectorEquality) || (this.GlobalProperties.Wire2dUseFontOutLines & vdRenderGlobalProperties.Wire2dUseFontOutLinesFlag.UseAPIWithScaleText) == vdRenderGlobalProperties.Wire2dUseFontOutLinesFlag.Default && !Globals.AreEqual(matrix2.Xdir.Length, matrix2.Ydir.Length, Globals.DefaultTextAPIEquality))
                    return;
                if (this.GlobalProperties.Wire2dSectionClip == vdRenderGlobalProperties.Wire2dSectionClipFlag.On && this.SectionClips != null && this.SectionClips.Count > 0)
                {
                    gPoints pts = box.TogPoints();
                    pts.makeClosed();
                    matrix2.Transform(pts);
                    this.StartedViewToWorldMatrix.Transform(pts);
                    if (this.SectionClips.CLipPoints(pts) != ClipTestFlag.CompletelyVisible)
                        return;
                }
                Graphics graphics = this.Display != vdRender.DisplayMode.SCREEN || this.GraphicsContext == null || this.MemoryBitmap != this.GraphicsContext.MemoryBitmap ? (this.Display != vdRender.DisplayMode.SCREEN_ACTION && this.Display != vdRender.DisplayMode.SCREEN_ACTION_HIGHLIGHT || this.GraphicsContext == null || this.MemoryBitmap != this.GraphicsContext.ActionBitmap ? this.graphics : this.GraphicsContext.ActionGraphics) : this.GraphicsContext.MemoryGraphics;
                if (graphics != null)
                {
                    System.Drawing.Drawing2D.Matrix matrix3 = matrix1.GetSystemMatrix();
                    if (this.IsPerspectiveModeOn)
                    {
                        gPoint gPoint = matrix1.projectTransform(new gPoint());
                        if (gPoint.z > 1.0 || gPoint.z < -1.0)
                            matrix3 = (System.Drawing.Drawing2D.Matrix)null;
                    }
                    if (matrix3 != null)
                    {
                        string str = new string((char*)chars.ToPointer(), 0, nchars);
                        grTextStyle fromGlobals = grTextStyle.FindFromGlobals(new string((char*)FontName.ToPointer(), 0, FontNameLength));
                        if (fromGlobals != null && fromGlobals.DrawFont != null)
                        {
                            string s = fromGlobals.HebrewReverse(str);
                            double PropertyValue = 0.0;
                            vdgl.GetPropertyValue(this.vdContext, vdglTypes.PropertyTypeGet.HIGHLIGHT, ref PropertyValue);
                            Color color = vdGdiPenStyle.FromArgb(colorRGBA[3], colorRGBA[0], colorRGBA[1], colorRGBA[2]);
                            Brush brush;
                            if (PropertyValue != 0.0)
                            {
                                Color alpha255BkColor = this.Alpha255BkColor;
                                if (color == alpha255BkColor)
                                    color = GDI.SystemFromRGB(16777215 ^ GDI.RGBFromSystem(color));
                                brush = (Brush)new HatchBrush(HatchStyle.DiagonalCross, color, alpha255BkColor);
                            }
                            else
                                brush = (Brush)new SolidBrush(color);
                            this.mIsQuickLock = true;
                            bool isLock = this.IsLock;
                            if (isLock)
                            {
                                if (this is ActionWrapperRender)
                                    ((ActionWrapperRender)this).UnLock(false);
                                else
                                    this.UnLock();
                            }
                            try
                            {
                                graphics.Transform = matrix3;
                                graphics.DrawString(s, new Font(fromGlobals.DrawFont.Name, fromGlobals.DrawFont.Size, fromGlobals.DrawFont.Style | (FontStyle)fontStyle, GraphicsUnit.Pixel), brush, 0.0f, 0.0f);
                            }
                            catch
                            {
                            }
                            graphics.Transform = new System.Drawing.Drawing2D.Matrix();
                            matrix3.Dispose();
                            if (isLock)
                                this.Lock();
                            this.mIsQuickLock = false;
                            Cancel |= 2;
                        }
                    }
                }
            }
            if (this.TestDrawBreak())
                Cancel |= 1;
            else
                this.TestTimerEvent();
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.DrawString(System.Object,VectorDraw.Render.grTextStyle,VectorDraw.Render.grTextStyleExtra,System.String,VectorDraw.Geometry.Box)" />
        /// </summary>
        public override void DrawString(
          object sender,
          grTextStyle style,
          grTextStyleExtra extra,
          string str,
          Box textBox)
        {
            if (this.TestDrawBreak())
                return;
            style.Calculate();
            grTextStyleExtra extra1 = extra ?? style.Extra;
            Box box = new Box(textBox != null ? textBox : style.GetBaseTextBox(str, extra1));
            if ((this.SupportLists & SupportListFlag.Create) == SupportListFlag.None && !this.IsPrinting && Math.Abs(this.CurrentMatrix.Properties.Scales.y) * style.Ascent / this.PixelSize < vdRenderGlobalProperties.MinTextHeight * (double)this.DpiY)
            {
                this.DrawSolidBoundBox((object)this, box);
                if (Globals.AreEqual(style.Thickness, 0.0, Globals.DefaultLinearEquality))
                    return;
                box.ExpandBy(style.Thickness);
                this.DrawBoundBox((object)this, box);
            }
            else
            {
                if (extra1.IsUnderLine)
                    this.DrawLine((object)this, new gPoint(box.Left, -style.Ascent * 0.2 - style.Ascent - style.TopLeftOffsetY), new gPoint(box.Right, -style.Ascent * 0.2 - style.Ascent - style.TopLeftOffsetY));
                if (extra1.IsStrikeOut)
                    this.DrawLine((object)this, new gPoint(box.Left, (box.Top + box.Bottom) / 2.0), new gPoint(box.Right, (box.Top + box.Bottom) / 2.0));
                if (extra1.IsOverLine)
                    this.DrawLine((object)this, new gPoint(box.Left, style.Ascent * 1.2 - style.Ascent - style.TopLeftOffsetY), new gPoint(box.Right, style.Ascent * 1.2 - style.Ascent - style.TopLeftOffsetY));
                char[] charArray = style.StringToCharArray(str, true);
                grTextStyle grTextStyle = style.FIllShapes((vdRender)this, this.vdContext, extra1, charArray);
                IntPtr zero = IntPtr.Zero;
                string str1 = grTextStyle.AsGlobalsString(extra1);
                int length = str1.Length;
                double ascent = grTextStyle.Ascent;
                int FontStyle = extra1.Bold ? 1 : 0;
                IntPtr num = Marshal.AllocCoTaskMem(length * 2);
                for (int index = 0; index < length; ++index)
                    Marshal.WriteInt16(num, index * 2, (short)str1[index]);
                int flag = grTextStyle.DrawFont != null ? 1 : 0;
                if ((this.GlobalProperties.OpenglUseFontOutLines & vdRenderGlobalProperties.OpenGLUseFontOutLinesFlag.DisableLighting) != vdRenderGlobalProperties.OpenGLUseFontOutLinesFlag.Default)
                    flag |= 65536;
                vdgl.Element_String_Init(this.vdContext, flag, num, length, (float)ascent, FontStyle, (float)style.Thickness, (float)box.Min.x, (float)box.Min.y, (float)box.Max.x, (float)box.Max.y);
                if (num != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(num);
                for (int index = 0; index < charArray.Length; ++index)
                {
                    char ch = charArray[index];
                    switch (ch)
                    {
                        case '\n':
                        case '\r':
                            continue;
                        default:
                            TrueTypeAnalyzer.ShapeInfo shape = grTextStyle.getShape(ch, extra1);
                            vdgl.Element_String_AddChar(this.vdContext, (ushort)ch, shape.listId, (float)shape.advance_x, (float)shape.advance_y);
                            continue;
                    }
                }
                vdgl.Element_String_Draw(this.vdContext);
                this.TestTimerEvent();
            }
        }

        /// <summary>
        /// overrides the <see cref="P:VectorDraw.Render.vdRender.IsCreatingList" />
        /// </summary>
        public override bool IsCreatingList => this.CurentListDepth > 0;

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.IsBoundaryClip(VectorDraw.Geometry.Box)" />
        /// </summary>
        public override bool IsBoundaryClip(Box bound)
        {
            if (this.IsCreatingList)
                return bound != null && !bound.IsEmpty;
            if (bound == null || bound.IsEmpty)
                return false;
            return bound.IsInfinity || bound.AlignToView || vdgl.TestClipBox(this.vdContext, bound.Min.x, bound.Min.y, bound.Min.z, bound.Max.x, bound.Max.y, bound.Max.z) == 1;
        }

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.TestTimerEvent" />
        /// </summary>
        public override void TestTimerEvent()
        {
            if (this.IsCreatingList)
                return;
            base.TestTimerEvent();
        }

        /// <summary>
        /// overrides the <see cref="P:VectorDraw.Render.vdRender.SupportSectionClips" />
        /// </summary>
        public override bool SupportSectionClips => this.RenderMode == vdRender.Mode.Wire2d ? this.GlobalProperties.Wire2dSectionClip == vdRenderGlobalProperties.Wire2dSectionClipFlag.On : this.RenderMode != vdRender.Mode.Wire2dGdiPlus;

        internal virtual bool TestDrawBreak() => !this.IsCreatingList && !this.IsMessageQueEmpty();

        /// <summary>
        /// Implements the <see cref="P:VectorDraw.Render.IRenderList.SupportLists" />
        /// </summary>
        public virtual SupportListFlag SupportLists => SupportListFlag.CreateAndDraw;

        /// <summary>
        /// Implements the <see cref="M:VectorDraw.Render.IRenderList.GenList" />
        /// </summary>
        public virtual IntPtr GenList() => vdgl.CreateList(this.vdContext);

        /// <summary>
        /// Implements the <see cref="M:VectorDraw.Render.IRenderList.NewList(VectorDraw.Render.IRenderListItem)" />
        /// </summary>
        public virtual void NewList(IRenderListItem listItem)
        {
            IntPtr drawingList = listItem.DrawingList;
            ++this.mListDepth;
            this.sectionApplyModelMatrix.Push(this.CurrentMatrix);
            this.PushToViewMatrix();
            vdgl.StartNewList(this.vdContext, drawingList);
        }

        /// <summary>
        /// Implements the <see cref="M:VectorDraw.Render.IRenderList.EndList(VectorDraw.Render.IRenderListItem)" />
        /// </summary>
        public virtual void EndList(IRenderListItem listItem)
        {
            IntPtr drawingList = listItem.DrawingList;
            vdgl.FinishList(this.vdContext);
            this.PopMatrix();
            this.sectionApplyModelMatrix.Pop();
            --this.mListDepth;
            if (this.mListDepth != 0 || this.IsAlignToViewOn || this.SectionClips == null)
                return;
            this.SectionClips.CreateSectionCoverFacesList((vdRender)this, this.vdContext, listItem);
        }

        /// <summary>
        /// Implements the <see cref="M:VectorDraw.Render.IRenderList.DrawList(VectorDraw.Render.IRenderListItem)" />
        /// </summary>
        public virtual int DrawList(IRenderListItem listItem)
        {
            if (listItem == null)
                return -1;
            IntPtr drawingList = listItem.DrawingList;
            return drawingList == IntPtr.Zero || vdgl.IsEmptyList(drawingList) ? -1 : vdgl.DrawList(this.vdContext, drawingList);
        }

        /// <summary>
        /// Implements the <see cref="M:VectorDraw.Render.IRenderList.ListDeleted(VectorDraw.Render.IRenderListItem)" />
        /// </summary>
        public virtual void ListDeleted(IRenderListItem fig)
        {
        }

        /// <summary>
        /// Implements the <see cref="M:VectorDraw.Render.IRenderList.DrawBindMappedImageList(VectorDraw.Render.IRenderListItem,VectorDraw.Render.ISupportdMappedImages)" />
        /// </summary>
        public virtual void DrawBindMappedImageList(
          IRenderListItem sender,
          ISupportdMappedImages polyface)
        {
            if (polyface == null)
                return;
            int numMappedImages = polyface.GetNumMappedImages();
            if (numMappedImages == 0 || this.IsBlendingOn || this.IsSelectingMode || this.ActiveHighLightFilter == vdRender.HighLightFilter.On || this.RenderMode != vdRender.Mode.Shade && this.RenderMode != vdRender.Mode.ShadeOn && this.RenderMode != vdRender.Mode.Render && this.RenderMode != vdRender.Mode.RenderOn)
                return;
            IntPtr drawingList = sender.DrawingList;
            bool flag = polyface is IPolyface && ((IPolyface)polyface).CanDrawMeshes((vdRender)this);
            if (drawingList == IntPtr.Zero && !flag)
                return;
            this.PushPenstyle(new vdGdiPenStyle());
            bool bvalue1 = this.EnableTexture(true);
            bool bvalue2 = this.EnableDepthBufferWrite(false);
            bool bvalue3 = this.EnableBufferId(false);
            for (int index = 0; index < numMappedImages; ++index)
            {
                IBindMappedImage mappedImageAt = polyface.GetMappedImageAt(index);
                if (mappedImageAt != null && !mappedImageAt.Deleted && mappedImageAt.ImageBind != null && mappedImageAt.Visible)
                {
                    if (!this.TestDrawBreak())
                    {
                        VectorDraw.Geometry.Matrix mat = new VectorDraw.Geometry.Matrix();
                        mat.ScaleMatrix(1.0, 1.0 / mappedImageAt.Aspect, 1.0);
                        mat.Multiply(mappedImageAt.MaterialMatrix);
                        mat.Invert();
                        double[] imageMatrix = vdgl.AsvdrawContextMatrix(mat);
                        vdgl.SetMaterial(this.vdContext, (int)byte.MaxValue, (int)byte.MaxValue, (int)byte.MaxValue, (int)byte.MaxValue, vdglTypes.PenWidthFlag.PIXEL, 0.0, IntPtr.Zero, 1.0, mappedImageAt.GetBindPtr((vdRender)this), imageMatrix, 0, (int)byte.MaxValue, (int)byte.MaxValue, (int)byte.MaxValue, vdglTypes.MATERIAL_FLAG.MAPPEDIMAGE);
                        vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.MATERIAL_LOCK, new double[1]
                        {
              (double) vdglTypes.PropertyValues.POLYGON_MODE_LINES.GetHashCode()
                        });
                        if (flag)
                        {
                            int num = (int)((IPolyface)polyface).DrawMeshes((vdRender)this);
                        }
                        else
                            this.DrawList(sender);
                        vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.MATERIAL_LOCK, new double[1]
                        {
              (double) vdglTypes.PropertyValues.TEXTURE_OFF.GetHashCode()
                        });
                        vdgl.SetMaterial(this.vdContext, (int)byte.MaxValue, (int)byte.MaxValue, (int)byte.MaxValue, (int)byte.MaxValue, vdglTypes.PenWidthFlag.PIXEL, 0.0, IntPtr.Zero, 1.0, IntPtr.Zero, imageMatrix, 0, (int)byte.MaxValue, (int)byte.MaxValue, (int)byte.MaxValue, vdglTypes.MATERIAL_FLAG.MAPPEDIMAGE);
                    }
                    else
                        break;
                }
            }
            this.EnableDepthBufferWrite(bvalue2);
            this.EnableTexture(bvalue1);
            this.EnableBufferId(bvalue3);
            this.PopPenstyle();
        }

        /// <summary>
        /// Implements the <see cref="P:VectorDraw.Render.IRenderList.CurentListDepth" />
        /// </summary>
        public virtual int CurentListDepth => this.mListDepth;

        /// <summary>Begings a new group with passed flag id.</summary>
        public void PushGroupIdFlag(vdrawglRender.GroupIdFlag flag) => vdgl.PushObjectId(this.vdContext, (int)flag);

        /// <summary>
        /// Ends the group that started by <see cref="M:VectorDraw.Render.vdrawglRender.PushGroupIdFlag(VectorDraw.Render.vdrawglRender.GroupIdFlag)" />
        /// </summary>
        public void PopGroupIdFlag() => vdgl.PopObjectId(this.vdContext);

        internal enum PenstyleFlag
        {
            None = 0,
            pushed = 128, // 0x00000080
            poped = 256, // 0x00000100
            ignoreLock = 512, // 0x00000200
        }

        /// <summary>This flag identifies a group of drawing elements.</summary>
        [Flags]
        public enum GroupIdFlag
        {
            /// <summary>
            /// Empty flag usually when drawing elements are not inside a group defined by <see cref="M:VectorDraw.Render.vdrawglRender.PushGroupIdFlag(VectorDraw.Render.vdrawglRender.GroupIdFlag)" /> and <see cref="M:VectorDraw.Render.vdrawglRender.PopGroupIdFlag" />
            /// </summary>
            None = 0,
            /// <summary>Defines a group of hatch block filled regions.</summary>
            HatchBlock = 1,
            /// <summary>
            /// Defines a group of hatch block with FillBkColor regions.
            /// </summary>
            FillBk = 2,
        }
    }
}
