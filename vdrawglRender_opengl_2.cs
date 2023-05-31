using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using VectorDraw.Geometry;
using VectorDraw.Professional.vdFigures;
using VectorDraw.Render;
using VectorDraw.Render.OpenGL;
using VectorDraw.WinMessages;

namespace vdShader
{
        internal class vdrawglRender_opengl_2 : vdrawglRender, IvdOpenGLRender, IvdOpenGLShared, IvdRender
    {
        private IntPtr mActiveBindImage = IntPtr.Zero;
        private bool mIsLockGL;
        private vdglTypes.lockStatus mLockStatus;
        private double mLineWidthMinimumDefault = 1.0;
        private bool mDrawStipple;
        private ControlDC ownctrl;
        private int mglStatus;
        private bool IsUnlock2;
        private glListItems mListItems = new glListItems();
        private IvdOpenGLShared mShared;
        private bool mContainsTrasparent;
        private bool mIslockToPixelMatrix;
        private Matrix mOffsetMat;
        private bool mEnableCoordCorrection;
        private gPoint mTmpViewCenter;
        private Matrix mTmpCurrentMatrix;
        private byte[] colormask = new byte[4];
        private OpenGLImports.AttribMask glattribmask2 = OpenGLImports.AttribMask.GL_COLOR_BUFFER_BIT;
        private OpenGLImports.AttribMask glattribmask = OpenGLImports.AttribMask.GL_POINT_BIT | OpenGLImports.AttribMask.GL_LINE_BIT | OpenGLImports.AttribMask.GL_POLYGON_BIT | OpenGLImports.AttribMask.GL_LIGHTING_BIT;
        private vdRender.ShadowLightModeFlag mShadowLightMode;
        private vdrawglRender_opengl_2.EdgePassFlag mEdgePass;

        public override bool IsOpenGLRender => this.defaultLockStatus == vdglTypes.lockStatus.OPENGL;

        public override IvdOpenGLRender OpenGLRender => this.IsOpenGLRender ? (IvdOpenGLRender)this : (IvdOpenGLRender)null;

        internal virtual vdglTypes.lockStatus defaultLockStatus => vdglTypes.lockStatus.OPENGL;

        public void SetShared(IvdOpenGLShared with)
        {
            if (with == this)
                this.mShared = (IvdOpenGLShared)null;
            else
                this.mShared = with;
        }

        public glListItems ListItems => this.mShared != null ? this.mShared.ListItems : this.mListItems;

        public vdRender BasicRender => (vdRender)this;

        public vdrawglRender_opengl_2(vdRender OriginalRender)
          : base(OriginalRender)
        {
        }

        internal override void UpdateRenderModeProperties()
        {
            base.UpdateRenderModeProperties();
            if (!this.mIsLockGL)
                return;
            OpenGLImports.glDisable(OpenGLImports.GLCap.GL_POLYGON_OFFSET_FILL);
            OpenGLImports.glPolygonMode(OpenGLImports.FaceMode.GL_FRONT_AND_BACK, OpenGLImports.PolygonMode.GL_FILL);
            OpenGLImports.glEdgeFlag((byte)0);
            OpenGLImports.glAlphaFunc(OpenGLImports.ComparisonFunction.GL_ALWAYS, 0.0f);
            OpenGLImports.glEnable(OpenGLImports.GLCap.GL_COLOR_MATERIAL);
            OpenGLImports.glStencilOp(OpenGLImports.StencilOp.GL_KEEP, OpenGLImports.StencilOp.GL_KEEP, OpenGLImports.StencilOp.GL_REPLACE);
            switch (this.RenderMode)
            {
                case vdRender.Mode.Wire3d:
                    if (this.SupportWire3d_Transparent)
                        OpenGLImports.glAlphaFunc(OpenGLImports.ComparisonFunction.GL_GEQUAL, 0.9999f);
                    OpenGLImports.glPolygonMode(OpenGLImports.FaceMode.GL_FRONT_AND_BACK, OpenGLImports.PolygonMode.GL_LINE);
                    if (this.GlobalProperties.OpenGLPolygonOffsetFill == vdRenderGlobalProperties.OpenGLPolygonOffsetFillFlag.AllModes)
                    {
                        OpenGLImports.glEnable(OpenGLImports.GLCap.GL_POLYGON_OFFSET_FILL);
                        break;
                    }
                    break;
                case vdRender.Mode.Hide:
                    this.EnableColorBuffer(false);
                    OpenGLImports.glEnable(OpenGLImports.GLCap.GL_POLYGON_OFFSET_FILL);
                    break;
                case vdRender.Mode.Shade:
                    OpenGLImports.glEdgeFlag((byte)1);
                    OpenGLImports.glAlphaFunc(OpenGLImports.ComparisonFunction.GL_GEQUAL, 0.9999f);
                    if (this.GlobalProperties.OpenGLPolygonOffsetFill == vdRenderGlobalProperties.OpenGLPolygonOffsetFillFlag.AllModes)
                    {
                        OpenGLImports.glEnable(OpenGLImports.GLCap.GL_POLYGON_OFFSET_FILL);
                        break;
                    }
                    break;
                case vdRender.Mode.ShadeOn:
                    OpenGLImports.glAlphaFunc(OpenGLImports.ComparisonFunction.GL_GEQUAL, 0.9999f);
                    OpenGLImports.glEnable(OpenGLImports.GLCap.GL_POLYGON_OFFSET_FILL);
                    break;
                case vdRender.Mode.Render:
                    OpenGLImports.glDisable(OpenGLImports.GLCap.GL_COLOR_MATERIAL);
                    OpenGLImports.glAlphaFunc(OpenGLImports.ComparisonFunction.GL_GEQUAL, 0.9999f);
                    if (this.GlobalProperties.OpenGLPolygonOffsetFill == vdRenderGlobalProperties.OpenGLPolygonOffsetFillFlag.AllModes)
                    {
                        OpenGLImports.glEnable(OpenGLImports.GLCap.GL_POLYGON_OFFSET_FILL);
                        break;
                    }
                    break;
                case vdRender.Mode.RenderOn:
                    OpenGLImports.glDisable(OpenGLImports.GLCap.GL_COLOR_MATERIAL);
                    OpenGLImports.glAlphaFunc(OpenGLImports.ComparisonFunction.GL_GEQUAL, 0.9999f);
                    OpenGLImports.glEnable(OpenGLImports.GLCap.GL_POLYGON_OFFSET_FILL);
                    break;
            }
            if (this.GlobalProperties.IgnoreTransparency)
                OpenGLImports.glDisable(OpenGLImports.GLCap.GL_ALPHA_TEST);
            this.SetLineWidth(this.mLineWidthMinimumDefault);
            this.UpdateEdgeOnLists();
        }

        private void PrepareRenderMode()
        {
            if (!this.mIsLockGL)
                return;
            OpenGLImports.glBindTexture(OpenGLImports.TargetTexture.GL_TEXTURE_2D, 0U);
            this.mActiveBindImage = IntPtr.Zero;
            OpenGLImports.glLightModelfv(OpenGLImports.LightModelParameter.GL_LIGHT_MODEL_AMBIENT, new float[4]
            {
        0.15f,
        0.15f,
        0.15f,
        1f
            });
            OpenGLImports.glLightModeli(OpenGLImports.LightModelParameter.GL_LIGHT_MODEL_TWO_SIDE, 1);
            OpenGLImports.glLightModeli(OpenGLImports.LightModelParameter.GL_LIGHT_MODEL_LOCAL_VIEWER, 1);
            OpenGLImports.glBlendFunc(OpenGLImports.BlendingFactorSrc.GL_SRC_ALPHA, OpenGLImports.BlendingFactorDest.GL_ONE_MINUS_SRC_ALPHA);
            OpenGLImports.glFrontFace(vdRenderGlobalProperties.IsFrontFaceClockWise ? OpenGLImports.FrontFaceDirection.GL_CW : OpenGLImports.FrontFaceDirection.GL_CCW);
            OpenGLImports.glDisable(OpenGLImports.GLCap.GL_DITHER);
            OpenGLImports.glDisable(OpenGLImports.GLCap.GL_TEXTURE_1D);
            OpenGLImports.glDisable(OpenGLImports.GLCap.GL_BLEND);
            OpenGLImports.glEnable(OpenGLImports.GLCap.GL_ALPHA_TEST);
            OpenGLImports.glDepthMask((byte)1);
            OpenGLImports.glDepthFunc(OpenGLImports.ComparisonFunction.GL_LEQUAL);
            OpenGLImports.glClearDepth(1.0);
            OpenGLImports.glPolygonOffset(0.5f, 0.5f);
            OpenGLImports.glColorMaterial(OpenGLImports.FaceMode.GL_FRONT_AND_BACK, OpenGLImports.ColorMaterialParameter.GL_AMBIENT_AND_DIFFUSE);
            OpenGLImports.glDisable(OpenGLImports.GLCap.GL_FOG);
            OpenGLImports.glDisable(OpenGLImports.GLCap.GL_INDEX_LOGIC_OP);
            OpenGLImports.glPixelStorei(OpenGLImports.PixelStore.GL_PACK_ALIGNMENT, 1);
            OpenGLImports.glPixelStorei(OpenGLImports.PixelStore.GL_UNPACK_ALIGNMENT, 1);
            OpenGLImports.glDisable(OpenGLImports.GLCap.GL_STENCIL_TEST);
            OpenGLImports.glDisable(OpenGLImports.GLCap.GL_AUTO_NORMAL);
            OpenGLImports.glDisable(OpenGLImports.GLCap.GL_CULL_FACE);
            OpenGLImports.glDisable(OpenGLImports.GLCap.GL_NORMALIZE);
            OpenGLImports.glEnable(OpenGLImports.GLCap.GL_RESCALE_NORMAL);
            OpenGLImports.glShadeModel(OpenGLImports.ShadingModel.GL_SMOOTH);
            OpenGLImports.glPolygonStipple(OpenGLImports.PolygonStipple_Hilight);
            OpenGLImports.glLineStipple(1, OpenGLImports.LineStipple);
            OpenGLImports.glDisable(OpenGLImports.GLCap.GL_POLYGON_STIPPLE);
            OpenGLImports.glDisable(OpenGLImports.GLCap.GL_LINE_STIPPLE);
            this.SetLineWidth(0.0);
            this.mLineWidthMinimumDefault = 1.0;
            if (this.GlobalProperties.LineDrawQualityMode == vdRender.RenderingQualityMode.HighQuality || this.GlobalProperties.RenderingQuality == vdRender.RenderingQualityMode.HighQuality)
            {
                OpenGLImports.glEnable(OpenGLImports.GLCap.GL_BLEND);
                OpenGLImports.glEnable(OpenGLImports.GLCap.GL_LINE_SMOOTH);
                OpenGLImports.glHint(OpenGLImports.HintTarget.GL_LINE_SMOOTH_HINT, OpenGLImports.HintMode.GL_NICEST);
                this.mLineWidthMinimumDefault = this.GlobalProperties.OpenGlAntializingWidth;
                OpenGLImports.glEnable(OpenGLImports.GLCap.GL_POINT_SMOOTH);
                OpenGLImports.glEnable(OpenGLImports.GLCap.GL_MULTISAMPLE_ARB);
            }
            else
            {
                OpenGLImports.glDisable(OpenGLImports.GLCap.GL_LINE_SMOOTH);
                OpenGLImports.glHint(OpenGLImports.HintTarget.GL_LINE_SMOOTH_HINT, OpenGLImports.HintMode.GL_FASTEST);
                this.mLineWidthMinimumDefault = 1.0;
                OpenGLImports.glDisable(OpenGLImports.GLCap.GL_POINT_SMOOTH);
                OpenGLImports.glDisable(OpenGLImports.GLCap.GL_MULTISAMPLE_ARB);
            }
            this.PrepareMatrixes();
            this.UpdateRenderModeProperties();
        }

        public override void PushHighLightFilter(vdRender.HighLightFilter nvalue)
        {
            vdRender.HighLightFilter activeHighLightFilter = this.ActiveHighLightFilter;
            base.PushHighLightFilter(nvalue);
            if (this.ActiveHighLightFilter != activeHighLightFilter)
                this.FlushDrawBuffers(-1);
            if (!this.mIsLockGL || !this.IsActiveHighLightFilterOn && !this.mDrawStipple)
                return;
            OpenGLImports.glEnable(OpenGLImports.GLCap.GL_POLYGON_STIPPLE);
            OpenGLImports.glEnable(OpenGLImports.GLCap.GL_LINE_STIPPLE);
        }

        public override void PopHighLightFilter()
        {
            vdRender.HighLightFilter activeHighLightFilter = this.ActiveHighLightFilter;
            base.PopHighLightFilter();
            if (this.ActiveHighLightFilter != activeHighLightFilter)
                this.FlushDrawBuffers(-1);
            if (!this.mIsLockGL || this.IsActiveHighLightFilterOn || this.mDrawStipple)
                return;
            OpenGLImports.glDisable(OpenGLImports.GLCap.GL_POLYGON_STIPPLE);
            OpenGLImports.glDisable(OpenGLImports.GLCap.GL_LINE_STIPPLE);
        }

        /// <summary>
        /// Se to the vdRender a boolean value either to use a stipple filter or not. Striple filter is used for the SectionClip extended highlight filter.
        /// </summary>
        /// <param name="bval">True or false to enable this stipple filter.</param>
        /// <returns> the previous selected stipple. </returns>
        public override bool SetDrawStipple(bool bval)
        {
            bool flag = base.SetDrawStipple(bval);
            if (flag != bval)
                this.FlushDrawBuffers(-1);
            if (!this.mIsLockGL)
                return flag;
            bool mDrawStipple = this.mDrawStipple;
            this.mDrawStipple = bval;
            if (!this.mDrawStipple)
                OpenGLImports.glPolygonStipple(OpenGLImports.PolygonStipple_Hilight);
            else
                OpenGLImports.glPolygonStipple(OpenGLImports.PolygonStipple);
            if (this.IsActiveHighLightFilterOn || this.mDrawStipple)
            {
                OpenGLImports.glEnable(OpenGLImports.GLCap.GL_POLYGON_STIPPLE);
                OpenGLImports.glEnable(OpenGLImports.GLCap.GL_LINE_STIPPLE);
            }
            else if (!this.IsActiveHighLightFilterOn && !this.mDrawStipple)
            {
                OpenGLImports.glDisable(OpenGLImports.GLCap.GL_POLYGON_STIPPLE);
                OpenGLImports.glDisable(OpenGLImports.GLCap.GL_LINE_STIPPLE);
            }
            return mDrawStipple;
        }

        public override bool EnableLighting(bool bVal)
        {
            bool flag = base.EnableLighting(bVal);
            if (flag != bVal)
                this.FlushDrawBuffers(-1);
            if (this.mIsLockGL)
            {
                if (bVal)
                    OpenGLImports.glEnable(OpenGLImports.GLCap.GL_LIGHTING);
                else
                    OpenGLImports.glDisable(OpenGLImports.GLCap.GL_LIGHTING);
            }
            return flag;
        }

        public override bool EnableTexture(bool bvalue)
        {
            bool flag = base.EnableTexture(bvalue);
            if (flag != bvalue)
                this.FlushDrawBuffers(-1);
            if (this.mIsLockGL)
            {
                if (bvalue)
                    OpenGLImports.glEnable(OpenGLImports.GLCap.GL_TEXTURE_2D);
                else
                    OpenGLImports.glDisable(OpenGLImports.GLCap.GL_TEXTURE_2D);
            }
            return flag;
        }

        public override bool EnableBufferId(bool bvalue)
        {
            bool flag = base.EnableBufferId(bvalue);
            if (flag != bvalue)
                this.FlushDrawBuffers(-1);
            if (this.mIsLockGL)
            {
                if (bvalue)
                    OpenGLImports.glEnable(OpenGLImports.GLCap.GL_STENCIL_TEST);
                else
                    OpenGLImports.glDisable(OpenGLImports.GLCap.GL_STENCIL_TEST);
            }
            return flag;
        }

        public override bool EnableDepthBuffer(bool bvalue)
        {
            bool flag = base.EnableDepthBuffer(bvalue);
            if (flag != bvalue)
                this.FlushDrawBuffers(-1);
            if (this.mIsLockGL)
            {
                if (bvalue)
                    OpenGLImports.glEnable(OpenGLImports.GLCap.GL_DEPTH_TEST);
                else
                    OpenGLImports.glDisable(OpenGLImports.GLCap.GL_DEPTH_TEST);
            }
            return flag;
        }

        public override bool EnableDepthBufferWrite(bool bvalue)
        {
            bool flag = base.EnableDepthBufferWrite(bvalue);
            if (flag != bvalue)
                this.FlushDrawBuffers(-1);
            if (this.mIsLockGL)
                OpenGLImports.glDepthMask(bvalue ? (byte)1 : (byte)0);
            return flag;
        }

        public override bool EnableColorBuffer(bool bvalue)
        {
            bool flag = base.EnableColorBuffer(bvalue);
            if (flag != bvalue)
                this.FlushDrawBuffers(-1);
            if (this.mIsLockGL)
                this.PuseSetColorMask(bvalue);
            return flag;
        }

        /// <summary>
        /// Internally used.overrides the <see cref="M:VectorDraw.Render.vdRender.ForceBlending(System.Boolean)" />
        /// </summary>
        public override bool ForceBlending(bool bForce)
        {
            bool flag = base.ForceBlending(bForce);
            if (flag != bForce)
                this.FlushDrawBuffers(-1);
            if (this.mIsLockGL)
            {
                if (this.mForceBlending)
                {
                    OpenGLImports.glEnable(OpenGLImports.GLCap.GL_BLEND);
                    OpenGLImports.glBlendFunc(OpenGLImports.BlendingFactorSrc.GL_SRC_ALPHA, OpenGLImports.BlendingFactorDest.GL_ONE_MINUS_SRC_ALPHA);
                    OpenGLImports.glEnable(OpenGLImports.GLCap.GL_ALPHA_TEST);
                    OpenGLImports.glAlphaFunc(OpenGLImports.ComparisonFunction.GL_ALWAYS, 0.0f);
                }
                else if (this.GlobalProperties.LineDrawQualityMode == vdRender.RenderingQualityMode.HighQuality || this.GlobalProperties.RenderingQuality == vdRender.RenderingQualityMode.HighQuality)
                    OpenGLImports.glEnable(OpenGLImports.GLCap.GL_BLEND);
                else
                    OpenGLImports.glDisable(OpenGLImports.GLCap.GL_BLEND);
            }
            return flag;
        }

        private void PrepareMatrixes()
        {
            if (!this.mIsLockGL)
                return;
            Point point = this.UpperLeft;
            int num1 = point.X;
            point = this.UpperLeft;
            int num2 = point.Y;
            if (this.LayoutRender != null && !this.mIslockToPixelMatrix)
            {
                point = this.OwnerGraphicsOffset;
                if (point.X < 0)
                {
                    int num3 = num1;
                    point = this.OwnerGraphicsOffset;
                    int x = point.X;
                    num1 = num3 + x;
                }
                point = this.OwnerGraphicsOffset;
                if (point.Y < 0)
                {
                    int num4 = num2;
                    point = this.OwnerGraphicsOffset;
                    int y = point.Y;
                    num2 = num4 + y;
                }
            }
            double a = 1.0;
            while ((double)this.Width / a > 6552.0 || (double)this.Height / a > 6552.0)
                ++a;
            int num5 = 0;
            int num6 = 0;
            if ((double)this.Width / a - (double)num1 > 3276.0 || (double)this.Height / a - (double)num2 > 3276.0)
            {
                num5 = num1;
                num6 = num2;
                num1 = 0;
                num2 = 0;
            }
            int[] numArray = new int[4]
            {
        num1,
        num2,
        (int) ((double) this.Width / a),
        (int) ((double) this.Height / a)
            };
            OpenGLImports.glViewport(numArray[0], numArray[1], numArray[2], numArray[3]);
            OpenGLImports.glMatrixMode(OpenGLImports.MatrixMode.GL_PROJECTION);
            Matrix mat = new Matrix(this.ProjectionMatrix);
            mat.ScaleMatrix(a, -a, 1.0);
            mat.TranslateMatrix(a - 1.0, a - 1.0, 0.0);
            mat.TranslateMatrix(2.0 * a * (double)num5 / (double)this.Width, 2.0 * a * (double)num6 / (double)this.Height, 0.0);
            OpenGLImports.glLoadMatrixd(OpenGLImports.GetGLMatrix(mat));
            OpenGLImports.glMatrixMode(OpenGLImports.MatrixMode.GL_TEXTURE);
            OpenGLImports.glLoadIdentity();
            OpenGLImports.glMatrixMode(OpenGLImports.MatrixMode.GL_MODELVIEW);
            OpenGLImports.glLoadIdentity();
            this.MatrixViewChanged();
        }

        public override void MatrixView2PixelChanged()
        {
            base.MatrixView2PixelChanged();
            this.PrepareMatrixes();
        }

        public override uint GetIdAtPixel(int x, int y)
        {
            if (!this.IsOpenGLRender)
                return base.GetIdAtPixel(x, y);
            uint idAtPixel = 0;
            if (this.IsDepthBufferEnable)
            {
                bool flag = this.IsLockOpenGLContect();
                if (!flag)
                    this.LockOpenGLContect();
                uint[] pixels = new uint[1];
                OpenGLImports.glReadPixelsUInt(x, y, 1, 1, OpenGLImports.PixelFormat.GL_STENCIL_INDEX, OpenGLImports.PixelType.GL_UNSIGNED_INT, pixels);
                idAtPixel = pixels[0];
                if (!flag)
                    this.UnLockOpenGLContect();
            }
            return idAtPixel;
        }

        public override double GetDepthAtPixel(int x, int y)
        {
            if (!this.IsOpenGLRender)
                return base.GetDepthAtPixel(x, y);
            double depthAtPixel = 0.0;
            if (this.IsDepthBufferEnable)
            {
                bool flag = this.IsLockOpenGLContect();
                if (!flag)
                    this.LockOpenGLContect();
                depthAtPixel = OpenGLImports.glReadPixelDepth(x, y);
                if (!flag)
                    this.UnLockOpenGLContect();
            }
            return depthAtPixel;
        }

        public override void Destroy(bool bFinalized)
        {
            base.Destroy(bFinalized);
            if (this.ownctrl != null)
                this.ownctrl.Reset();
            this.ownctrl = (ControlDC)null;
        }

        internal IopenglControl OpenGLControlObject
        {
            get
            {
                if (this.OwnerObject is IUseOwnGLContext ownerObject && ownerObject.IsOwnGlContext)
                {
                    Size size = new Size(this.Width, this.Height);
                    if (this.ownctrl == null)
                    {
                        this.ownctrl = new ControlDC(size);
                    }
                    else
                    {
                        if (this.ownctrl.NeedSizeUpdate(size))
                            this.ListItems.ClearAllLists((IopenglControl)this.ownctrl);
                        this.ownctrl.EnsureOpenGLContext(size);
                    }
                    return (IopenglControl)this.ownctrl;
                }
                return this.ownctrl != null ? (IopenglControl)this.ownctrl : (IopenglControl)ControlDC.ctrlDC;
            }
        }

        public bool LockOpenGLContect() => this.OpenGLControlObject != null && this.OpenGLControlObject.LockOpenGLContect();

        public void UnLockOpenGLContect()
        {
            if (this.OpenGLControlObject == null)
                return;
            this.FlushDrawBuffers(-1);
            this.OpenGLControlObject.UnLockOpenGLContect();
        }

        public IntPtr OpenGL_DC => this.OpenGLControlObject == null ? IntPtr.Zero : this.OpenGLControlObject.HDC;

        /// <summary>
        /// Internally used only.Draw the Selected MemoryBitmap Pixels to opengl context.
        /// </summary>
        public void UpdatePixels(BitmapData bmpdata, double x, double y)
        {
            if (!this.IsLockOpenGLContect())
                return;
            if (bmpdata == null)
            {
                bmpdata = this.bmpData;
                x = 0.0;
                y = 0.0;
            }
            if (bmpdata == null)
                return;
            this.FlushDrawBuffers(-1);
            int width = bmpdata.Width;
            int height = bmpdata.Height;
            ISectionClips sectionClips = this.SectionClips;
            this.ClearAllSectionClips();
            bool bvalue1 = this.EnableDepthBuffer(false);
            bool bvalue2 = this.EnableBufferId(false);
            bool bvalue3 = this.EnableColorBuffer(true);
            bool bvalue4 = this.EnableTexture(false);
            OpenGLImports.glMatrixMode(OpenGLImports.MatrixMode.GL_PROJECTION);
            OpenGLImports.glPushMatrix();
            OpenGLImports.glLoadIdentity();
            OpenGLImports.gluOrtho2D(0.0, (double)this.MemoryBitmap.Width, 0.0, (double)this.MemoryBitmap.Height);
            OpenGLImports.glMatrixMode(OpenGLImports.MatrixMode.GL_MODELVIEW);
            OpenGLImports.glPushMatrix();
            OpenGLImports.glLoadIdentity();
            OpenGLImports.TryglRasterPos(x, y, 0.0, 1.0);
            OpenGLImports.glDrawPixels(width, height, OpenGLImports.PixelFormat.GL_BGRA, OpenGLImports.PixelType.GL_UNSIGNED_BYTE, bmpdata.Scan0);
            OpenGLImports.TryglRasterPos(0.0, 0.0, 0.0, 1.0);
            OpenGLImports.glMatrixMode(OpenGLImports.MatrixMode.GL_PROJECTION);
            OpenGLImports.glPopMatrix();
            OpenGLImports.glMatrixMode(OpenGLImports.MatrixMode.GL_MODELVIEW);
            OpenGLImports.glPopMatrix();
            this.EnableTexture(bvalue4);
            this.EnableColorBuffer(bvalue3);
            this.EnableDepthBuffer(bvalue1);
            this.EnableBufferId(bvalue2);
            this.ApplySectionClips(sectionClips);
        }

        public override bool IsContextCreated => true;

        internal override bool SupportEdgeRender
        {
            get
            {
                if ((this.RenderMode == vdRender.Mode.Hide || this.RenderMode == vdRender.Mode.ShadeOn || this.RenderMode == vdRender.Mode.RenderOn) && this.GlobalProperties.EdgeEffect == vdRenderGlobalProperties.EdgeEffectFlag.DashedHidden)
                    return base.SupportEdgeRender;
                return this.mShadowLightMode != vdRender.ShadowLightModeFlag.ApplyShadow && this.mShadowLightMode != vdRender.ShadowLightModeFlag.CreateShadow && this.mEdgePass == vdrawglRender_opengl_2.EdgePassFlag.None && (this.RenderMode != vdRender.Mode.Hide && this.RenderMode != vdRender.Mode.ShadeOn && this.RenderMode != vdRender.Mode.RenderOn || this.GlobalProperties.EdgeEffect == vdRenderGlobalProperties.EdgeEffectFlag.UserDefine && this.mEdgePass == vdrawglRender_opengl_2.EdgePassFlag.None) && base.SupportEdgeRender;
            }
        }

        private bool IsEdgeColorEmpty => this.EdgeColor.A == (byte)0;

        private void SetEdgeOnDefaultPenStyle()
        {
            if (!this.mIsLockGL || !this.IsDrawEdgeOn)
                return;
            this.SetLineWidth((double)this.DpiY * (double)this.GlobalProperties.EdgePenWidth);
            if (this.IsEdgeColorEmpty)
                return;
            this.SetColor(this.EdgeColor);
        }

        public override bool StartEdgeRender()
        {
            this.FlushDrawBuffers(-1);
            bool flag = base.StartEdgeRender();
            if (flag && this.mIsLockGL)
            {
                OpenGLImports.glPolygonMode(OpenGLImports.FaceMode.GL_FRONT_AND_BACK, OpenGLImports.PolygonMode.GL_LINE);
                OpenGLImports.glDisable(OpenGLImports.GLCap.GL_POLYGON_OFFSET_FILL);
                OpenGLImports.glDisable(OpenGLImports.GLCap.GL_ALPHA_TEST);
                this.SetEdgeOnDefaultPenStyle();
                this.UpdateEdgeOnLists();
            }
            return flag;
        }

        public override void StopEdgeRender()
        {
            this.FlushDrawBuffers(-1);
            base.StopEdgeRender();
            if (!this.mIsLockGL)
                return;
            OpenGLImports.glEnable(OpenGLImports.GLCap.GL_ALPHA_TEST);
            if (!this.GlobalProperties.IgnoreTransparency)
                return;
            OpenGLImports.glDisable(OpenGLImports.GLCap.GL_ALPHA_TEST);
        }

        internal override bool SupportAlphaBlending
        {
            get
            {
                double PropertyValue = 0.0;
                if (this.vdContext != IntPtr.Zero)
                    vdgl.GetPropertyValue(this.vdContext, vdglTypes.PropertyTypeGet.HAS_TRANSPARENT, ref PropertyValue);
                if (!base.SupportAlphaBlending)
                    return false;
                return this.mContainsTrasparent || this.ListItems.mgllists_WithTransparent.Count > 0 || ((uint)(int)PropertyValue & 1U) > 0U;
            }
        }

        public override bool StartBlendingRender()
        {
            this.FlushDrawBuffers(-1);
            if (this.mEdgePass != vdrawglRender_opengl_2.EdgePassFlag.None || this.mShadowLightMode == vdRender.ShadowLightModeFlag.ApplyShadow || this.mShadowLightMode == vdRender.ShadowLightModeFlag.CreateShadow)
                return false;
            bool flag = base.StartBlendingRender();
            if (flag && this.mIsLockGL)
            {
                OpenGLImports.glAlphaFunc(OpenGLImports.ComparisonFunction.GL_GEQUAL, 0.9999f);
                OpenGLImports.glDepthFunc(OpenGLImports.ComparisonFunction.GL_LEQUAL);
                OpenGLImports.glDepthMask((byte)1);
                this.PuseSetColorMask(true);
                if (this.GlobalProperties.LineDrawQualityMode == vdRender.RenderingQualityMode.HighQuality || this.GlobalProperties.RenderingQuality == vdRender.RenderingQualityMode.HighQuality)
                    OpenGLImports.glEnable(OpenGLImports.GLCap.GL_BLEND);
                else
                    OpenGLImports.glDisable(OpenGLImports.GLCap.GL_BLEND);
            }
            return flag;
        }

        public override void SetBlendDrawMode(bool isFront)
        {
            this.FlushDrawBuffers(-1);
            base.SetBlendDrawMode(isFront);
            if (!this.mIsLockGL)
                return;
            if (isFront)
            {
                OpenGLImports.glAlphaFunc(OpenGLImports.ComparisonFunction.GL_LESS, 0.9999f);
                OpenGLImports.glDepthFunc(OpenGLImports.ComparisonFunction.GL_LEQUAL);
                OpenGLImports.glDepthMask((byte)1);
                this.PuseSetColorMask(false);
                OpenGLImports.glDisable(OpenGLImports.GLCap.GL_BLEND);
            }
            else
            {
                OpenGLImports.glAlphaFunc(OpenGLImports.ComparisonFunction.GL_LESS, 0.9999f);
                OpenGLImports.glDepthFunc(OpenGLImports.ComparisonFunction.GL_EQUAL);
                OpenGLImports.glDepthMask((byte)0);
                this.PuseSetColorMask(true);
                OpenGLImports.glEnable(OpenGLImports.GLCap.GL_BLEND);
            }
        }

        public override void StopBlendingRender()
        {
            this.FlushDrawBuffers(-1);
            if (this.mIsLockGL)
            {
                OpenGLImports.glAlphaFunc(OpenGLImports.ComparisonFunction.GL_ALWAYS, 0.0f);
                OpenGLImports.glDepthFunc(OpenGLImports.ComparisonFunction.GL_LEQUAL);
                OpenGLImports.glDepthMask((byte)1);
                this.PuseSetColorMask(true);
                if (this.GlobalProperties.LineDrawQualityMode == vdRender.RenderingQualityMode.HighQuality || this.GlobalProperties.RenderingQuality == vdRender.RenderingQualityMode.HighQuality)
                    OpenGLImports.glEnable(OpenGLImports.GLCap.GL_BLEND);
                else
                    OpenGLImports.glDisable(OpenGLImports.GLCap.GL_BLEND);
            }
            base.StopBlendingRender();
        }

        /// <summary>Returns a status code for diagnostic purposes.</summary>
        public override int Status => this.mglStatus;

        /// <summary>forces execution of OpenGL functions in finite time</summary>
        /// <param name="mode">
        /// Flush execution type 0 or -1.
        /// Set it to 0 in order to flush all executions
        /// Set it to -1 in order to flush all drawing primitives in memory buffer.
        /// </param>
        public void Flush(int mode)
        {
            if (this.mLockStatus == vdglTypes.lockStatus.None)
                return;
            this.FlushDrawBuffers(-1);
            if (!this.mIsLockGL || mode != 0)
                return;
            OpenGLImports.glFlush();
        }

        public override void LockToPixelMatrix()
        {
            this.FlushDrawBuffers(-1);
            this.mIslockToPixelMatrix = true;
            base.LockToPixelMatrix();
        }

        public override void UnLockToPixelMatrix()
        {
            this.FlushDrawBuffers(-1);
            this.mIslockToPixelMatrix = false;
            base.UnLockToPixelMatrix();
        }

        internal void FlushDrawBuffers(int mode)
        {
            if (this.mLockStatus == vdglTypes.lockStatus.None)
                return;
            vdgl.FlushDrawBuffers(this.vdContext, mode);
        }

        private void pureLock(bool registerfunctions)
        {
            if (registerfunctions)
                this.RegisterFunctionOverrride(true);
            this.mLockStatus = vdgl.LockGL(this.vdContext, this.defaultLockStatus);
        }

        private void DiagnosticDraw()
        {
            if (this.mLockStatus == vdglTypes.lockStatus.None || !this.GlobalProperties.CustomRenderTypeName.Contains("drawline"))
                return;
            bool bvalue1 = this.EnableDepthBuffer(false);
            bool bvalue2 = this.EnableDepthBufferWrite(false);
            bool bVal = this.EnableLighting(false);
            OpenGLImports.glMatrixMode(OpenGLImports.MatrixMode.GL_MODELVIEW);
            OpenGLImports.glPushMatrix();
            OpenGLImports.glLoadIdentity();
            gPoint minPoint = this.GetMinPoint();
            gPoint maxPoint = this.GetMaxPoint();
            if (this.mLockStatus == vdglTypes.lockStatus.OPENGL)
            {
                if (this.GlobalProperties.OpenGLDoubleBuffered)
                    OpenGLImports.glColor4d(1.0, 0.0, 0.0, 1.0);
                else
                    OpenGLImports.glColor4d(0.0, 0.0, 1.0, 1.0);
            }
            else
                OpenGLImports.glColor4d(0.0, 1.0, 0.0, 1.0);
            OpenGLImports.glBegin(OpenGLImports.Primitives.GL_LINES);
            OpenGLImports.glVertex3d(minPoint.x, minPoint.y, minPoint.z);
            OpenGLImports.glVertex3d(maxPoint.x, maxPoint.y, minPoint.z);
            OpenGLImports.glEnd();
            OpenGLImports.glPopMatrix();
            this.EnableLighting(bVal);
            this.EnableDepthBufferWrite(bvalue2);
            this.EnableDepthBuffer(bvalue1);
        }

        private void pureUnlock(bool Unregisterfunctions)
        {
            this.DiagnosticDraw();
            vdgl.UnLockGL(this.vdContext);
            if (Unregisterfunctions)
                this.RegisterFunctionOverrride(false);
            this.mLockStatus = vdglTypes.lockStatus.None;
            this.mIsLockGL = false;
        }

        private void RegisterFunctionOverrride(bool bregister)
        {
            if (bregister)
            {
                this.RegisterFunction(vdglTypes.FunctionType.ENUM_IMAGE_BIND);
                this.RegisterFunction(vdglTypes.FunctionType.ENUM_IMAGE_BIND_CREATED);
                this.RegisterFunction(vdglTypes.FunctionType.DRAWARRAYS);
                this.RegisterFunction(vdglTypes.FunctionType.DRAWMESH);
            }
            else
            {
                this.UnRegisterFunction(vdglTypes.FunctionType.ENUM_IMAGE_BIND);
                this.UnRegisterFunction(vdglTypes.FunctionType.ENUM_IMAGE_BIND_CREATED);
                this.UnRegisterFunction(vdglTypes.FunctionType.DRAWARRAYS);
                this.UnRegisterFunction(vdglTypes.FunctionType.DRAWMESH);
            }
        }

        public bool IsLockOpenGLContect() => this.OpenGLControlObject != null && this.OpenGLControlObject.IsLock();

        public bool ShadowSupported => this.OpenGLControlObject != null && this.SupportLights && this.OpenGLControlObject.ShadowSupported;

        /// <summary>
        /// De-activates opengl render and activates default VectorDraw rendering engine.
        /// </summary>
        public void UnlockTovdRender()
        {
            if (!this.mIsLockGL)
                return;
            this.UnLock();
            base.Lock();
            this.EnableDepthBuffer(false);
            this.EnableColorBuffer(true);
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.TRANSPARENT_ORDER, new double[1]
            {
        (double) vdglTypes.PropertyValues.TEXTURE_OFF.GetHashCode()
            });
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.DISABLESECTIONS, new double[1]
            {
        1.0
            });
            this.IsUnlock2 = true;
        }

        /// <summary>
        /// Re-activates opengl render that previous was de-activated by <see cref="M:vdShader.vdrawglRender_opengl_2.UnlockTovdRender" /> call
        /// </summary>
        public void LockTovdRender()
        {
            if (this.mIsLockGL)
                return;
            this.IsUnlock2 = false;
            base.UnLock();
            this.Lock();
            this.UpdateRenderModeProperties();
            OpenGLImports.glPushAttrib(OpenGLImports.AttribMask.GL_COLOR_BUFFER_BIT);
            OpenGLImports.glDisable(OpenGLImports.GLCap.GL_ALPHA_TEST);
            this.UpdatePixels((BitmapData)null, 0.0, 0.0);
            OpenGLImports.glPopAttrib();
        }

        public override void Lock()
        {
            base.Lock();
            if (this.IsUnlock2)
                return;
            this.pureLock(true);
            if (this.mLockStatus != vdglTypes.lockStatus.OPENGL)
                return;
            if (this.LockOpenGLContect())
            {
                this.mIsLockGL = true;
                this.mglStatus |= 1;
                OpenGLImports.glVersionString();
            }
            else
                this.pureUnlock(true);
        }

        public override unsafe void UnLock()
        {
            if (!this.IsUnlock2)
            {
                if (this.mLockStatus == vdglTypes.lockStatus.OPENGL)
                {
                    this.pureUnlock(true);
                    if (this.IsLockOpenGLContect())
                    {
                        OpenGLImports.glFinish();
                        OpenGLImports.glFlush();
                        int num1 = this.bmpData.Stride / this.bmpData.Width;
                        Point upperLeft = this.UpperLeft;
                        int x = upperLeft.X;
                        upperLeft = this.UpperLeft;
                        int y = upperLeft.Y;
                        int width = this.bmpData.Width;
                        int height = this.bmpData.Height;
                        int format = num1 == 3 ? 32992 : 32993;
                        IntPtr scan0_1 = this.bmpData.Scan0;
                        OpenGLImports.glReadPixels(x, y, width, height, (OpenGLImports.PixelFormat)format, OpenGLImports.PixelType.GL_UNSIGNED_BYTE, scan0_1);
                        if (num1 == 4)
                        {
                            byte* scan0_2 = (byte*)(void*)this.bmpData.Scan0;
                            int num2 = this.bmpData.Height * this.bmpData.Stride;
                            for (int index = 3; index < num2; index += 4)
                                scan0_2[index] = byte.MaxValue;
                        }
                        this.UnLockOpenGLContect();
                    }
                }
                else
                    this.pureUnlock(true);
            }
            base.UnLock();
        }

        public override void ApplyColorPalette()
        {
            this.FlushDrawBuffers(-1);
            if (!this.mIsLockGL || this.ColorPalette == vdRender.ColorDisplay.TrueColor && this.RenderingFilter == vdRender.RenderingFilterFlag.None || this.bmpData == null || this.bmpData.Stride / this.bmpData.Width != 4)
                base.ApplyColorPalette();
            else
                OpenGLImports.GLShaders.ApplyFilter((vdRender)this, this.bmpData);
        }

        public override void ClearDepthBuffer()
        {
            base.ClearDepthBuffer();
            if (!this.mIsLockGL)
                return;
            OpenGLImports.glClear(OpenGLImports.ClearMask.GL_DEPTH_BUFFER_BIT);
        }

        public override void Clear(Color color, bool applyGradient)
        {
            bool flag = applyGradient && !vdRender.IsColorEmpty(this.BkGradientColor);
            Color color1;
            if (flag || vdRender.IsColorEmpty(color))
            {
                base.Clear(color, applyGradient);
            }
            else
            {
                this.BkColor = color;
                if (this.Palette != null)
                {
                    if (!vdRender.IsColorEmpty(color))
                        this.Palette.SetBkColorFixForground(this.BkColor);
                    vdglTypes.SetPropertyValue_delegate setPropertyValue1 = vdgl.SetPropertyValue;
                    IntPtr vdContext1 = this.vdContext;
                    double[] PropertyValue1 = new double[4]
                    {
            (double) this.Palette.Forground.R,
            0.0,
            0.0,
            0.0
                    };
                    color1 = this.Palette.Forground;
                    PropertyValue1[1] = (double)color1.G;
                    color1 = this.Palette.Forground;
                    PropertyValue1[2] = (double)color1.B;
                    color1 = this.Palette.Forground;
                    PropertyValue1[3] = (double)color1.A;
                    setPropertyValue1(vdContext1, vdglTypes.PropertyType.FORGROUND, PropertyValue1);
                    vdglTypes.SetPropertyValue_delegate setPropertyValue2 = vdgl.SetPropertyValue;
                    IntPtr vdContext2 = this.vdContext;
                    double[] PropertyValue2 = new double[4];
                    color1 = this.Palette.Background;
                    PropertyValue2[0] = (double)color1.R;
                    color1 = this.Palette.Background;
                    PropertyValue2[1] = (double)color1.G;
                    color1 = this.Palette.Background;
                    PropertyValue2[2] = (double)color1.B;
                    color1 = this.Palette.Background;
                    PropertyValue2[3] = (double)color1.A;
                    setPropertyValue2(vdContext2, vdglTypes.PropertyType.BACKGROUND, PropertyValue2);
                }
            }
            if (!this.mIsLockGL)
                return;
            this.mIsClearColorEmpty = false;
            OpenGLImports.glClearStencil(0);
            OpenGLImports.glClear(OpenGLImports.ClearMask.GL_STENCIL_BUFFER_BIT);
            OpenGLImports.glClear(OpenGLImports.ClearMask.GL_DEPTH_BUFFER_BIT);
            bool bvalue1 = this.EnableColorBuffer(true);
            if (!flag && !vdRender.IsColorEmpty(color))
            {
                OpenGLImports.glClearColor((float)color.R / (float)byte.MaxValue, (float)color.G / (float)byte.MaxValue, (float)color.B / (float)byte.MaxValue, (float)color.A / (float)byte.MaxValue);
                OpenGLImports.glClear(OpenGLImports.ClearMask.GL_COLOR_BUFFER_BIT);
            }
            else
            {
                bool bvalue2 = this.EnableDepthBuffer(false);
                bool bvalue3 = this.EnableBufferId(false);
                bool bvalue4 = this.EnableTexture(false);
                int[] pparams = new int[4];
                OpenGLImports.glGetIntegerv(OpenGLImports.Parameters.GL_VIEWPORT, pparams);
                OpenGLImports.glViewport(0, 0, this.MemoryBitmap.Width, this.MemoryBitmap.Height);
                OpenGLImports.glMatrixMode(OpenGLImports.MatrixMode.GL_PROJECTION);
                OpenGLImports.glPushMatrix();
                OpenGLImports.glLoadIdentity();
                OpenGLImports.gluOrtho2D(0.0, (double)this.MemoryBitmap.Width, 0.0, (double)this.MemoryBitmap.Height);
                OpenGLImports.glMatrixMode(OpenGLImports.MatrixMode.GL_MODELVIEW);
                OpenGLImports.glPushMatrix();
                OpenGLImports.glLoadIdentity();
                OpenGLImports.TryglRasterPos(0.0, 0.0, 0.0, 1.0);
                if (!vdRender.IsColorEmpty(color) || this.LayoutRender == null || this.LayoutRender.IsLock)
                {
                    OpenGLImports.glDrawPixels(this.MemoryBitmap.Width, this.MemoryBitmap.Height, OpenGLImports.PixelFormat.GL_BGRA, OpenGLImports.PixelType.GL_UNSIGNED_BYTE, this.bmpData.Scan0);
                }
                else
                {
                    Bitmap memoryBitmap = this.LayoutRender.MemoryBitmap;
                    if (memoryBitmap == null)
                    {
                        color1 = this.BkColor;
                        double red = (double)color1.R / (double)byte.MaxValue;
                        color1 = this.BkColor;
                        double green = (double)color1.G / (double)byte.MaxValue;
                        color1 = this.BkColor;
                        double blue = (double)color1.B / (double)byte.MaxValue;
                        color1 = this.BkColor;
                        double alpha = (double)color1.A / (double)byte.MaxValue;
                        OpenGLImports.glClearColor((float)red, (float)green, (float)blue, (float)alpha);
                        OpenGLImports.glClear(OpenGLImports.ClearMask.GL_COLOR_BUFFER_BIT);
                    }
                    else
                    {
                        Rectangle rect1 = new Rectangle(Math.Max(0, this.OwnerGraphicsOffset.X), Math.Max(0, this.OwnerGraphicsOffset.Y), this.MemoryBitmap.Width, this.MemoryBitmap.Height);
                        Rectangle rect2 = new Rectangle(0, 0, memoryBitmap.Width, memoryBitmap.Height);
                        rect1.Intersect(rect2);
                        if (!rect1.IsEmpty)
                        {
                            BitmapData bitmapdata = memoryBitmap.LockBits(rect1, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                            OpenGLImports.glDrawPixels(rect1.Width, rect1.Height, OpenGLImports.PixelFormat.GL_BGRA, OpenGLImports.PixelType.GL_UNSIGNED_BYTE, bitmapdata.Scan0);
                            memoryBitmap.UnlockBits(bitmapdata);
                        }
                    }
                }
                this.EnableTexture(bvalue4);
                this.EnableDepthBuffer(bvalue2);
                this.EnableBufferId(bvalue3);
                OpenGLImports.glMatrixMode(OpenGLImports.MatrixMode.GL_PROJECTION);
                OpenGLImports.glPopMatrix();
                OpenGLImports.glMatrixMode(OpenGLImports.MatrixMode.GL_MODELVIEW);
                OpenGLImports.glPopMatrix();
                OpenGLImports.glViewport(pparams[0], pparams[1], pparams[2], pparams[3]);
                OpenGLImports.TryglRasterPos(0.0, 0.0, 0.0, 1.0);
            }
            this.EnableColorBuffer(bvalue1);
            this.UpdateListColors();
        }

        public override void ApplySectionClips(ISectionClips sections)
        {
            if (this.SectionClips != null && this.SectionClips.Count > 0 || sections != null && sections.Count > 0)
                this.FlushDrawBuffers(-1);
            base.ApplySectionClips(sections);
            if (!this.mIsLockGL || sections == null)
                return;
            int num = 0;
            for (int index = 0; index < sections.Count && num <= 5; ++index)
            {
                ISectionClip sectionClip = sections.GetItem(index);
                if (sectionClip.Enable)
                {
                    gPoint gPoint = sectionClip.OriginPoint.Clone() as gPoint;
                    Vector vector = sectionClip.Direction.Clone() as Vector;
                    double[] equation = new double[4]
                    {
            vector.x,
            vector.y,
            vector.z,
            0.0
                    };
                    this.PushOffsetMatrix(true);
                    if (this.mEnableCoordCorrection && this.GetOffsetMat != (Matrix)null)
                        gPoint -= this.GetOffsetPt;
                    OpenGLImports.glTranslated(gPoint.x, gPoint.y, gPoint.z);
                    OpenGLImports.glClipPlane((OpenGLImports.ClipPlaneName)(12288 + num), equation);
                    OpenGLImports.glEnable((OpenGLImports.GLCap)(12288 + num));
                    OpenGLImports.glTranslated(-gPoint.x, -gPoint.y, -gPoint.z);
                    this.PopOffsetMatrix(true);
                    ++num;
                }
            }
        }

        public override void ClearAllSectionClips()
        {
            if (this.SectionClips != null && this.SectionClips.Count > 0)
                this.FlushDrawBuffers(-1);
            if (this.mIsLockGL)
            {
                for (int index = 0; index < 6; ++index)
                    OpenGLImports.glDisable((OpenGLImports.GLCap)(12288 + index));
            }
            base.ClearAllSectionClips();
        }

        public override void DrawLight(object sender, IRenderingLight light)
        {
            if (!this.SupportLights || !this.mIsLockGL)
            {
                base.DrawLight(sender, light);
            }
            else
            {
                int lightIndex = this.LManager.GetLightIndex(light);
                if (lightIndex < 0)
                    return;
                Vector v = new Vector(light.Direction);
                gPoint gPoint = new gPoint(light.Position);
                float intensityValue = (float)light.IntensityValue;
                float[] pparams1 = new float[4]
                {
          intensityValue / (float) byte.MaxValue,
          intensityValue / (float) byte.MaxValue,
          intensityValue / (float) byte.MaxValue,
          1f
                };
                float[] pparams2 = new float[4]
                {
          (float) light.color.R / (float) byte.MaxValue,
          (float) light.color.G / (float) byte.MaxValue,
          (float) light.color.B / (float) byte.MaxValue,
          1f
                };
                float[] pparams3 = new float[4] { 1f, 1f, 1f, 1f };
                if (light.ApplyShadow && this.mShadowLightMode == vdRender.ShadowLightModeFlag.RenderScene)
                {
                    pparams1[0] *= 0.2f;
                    pparams1[1] *= 0.2f;
                    pparams1[2] *= 0.2f;
                    pparams2[0] *= 0.2f;
                    pparams2[1] *= 0.2f;
                    pparams2[2] *= 0.2f;
                    pparams3[0] = 0.0f;
                    pparams3[1] = 0.0f;
                    pparams3[2] = 0.0f;
                }
                Vector nearFarHeight = this.GetNearFarHeight();
                if (light.GlobalLight)
                {
                    this.View2Worldmatrix.TransformVector(v, false);
                    double z = 0.0;
                    if (!this.IsPerspectiveModeOn)
                        z = nearFarHeight.x + (nearFarHeight.x - nearFarHeight.y) * 0.1;
                    gPoint = this.View2Worldmatrix.Transform(0.0, 0.0, z);
                }
                if (light.TypeOfLight == LightType.Directional)
                    v *= -1.0;
                OpenGLImports.LightName lightName = (OpenGLImports.LightName)(16384 + lightIndex);
                if (light.TypeOfLight == LightType.Positional)
                    OpenGLImports.glLightfv(lightName, OpenGLImports.LightParameter.GL_POSITION, new float[4]
                    {
            (float) gPoint.x,
            (float) gPoint.y,
            (float) gPoint.z,
            1f
                    });
                else if (light.TypeOfLight == LightType.Directional)
                    OpenGLImports.glLightfv(lightName, OpenGLImports.LightParameter.GL_POSITION, new float[4]
                    {
            (float) v.x,
            (float) v.y,
            (float) v.z,
            0.0f
                    });
                else if (light.TypeOfLight == LightType.Spot)
                {
                    OpenGLImports.glLightfv(lightName, OpenGLImports.LightParameter.GL_POSITION, new float[4]
                    {
            (float) gPoint.x,
            (float) gPoint.y,
            (float) gPoint.z,
            1f
                    });
                    OpenGLImports.glLightfv(lightName, OpenGLImports.LightParameter.GL_SPOT_DIRECTION, new float[3]
                    {
            (float) v.x,
            (float) v.y,
            (float) v.z
                    });
                }
                OpenGLImports.glLightfv(lightName, OpenGLImports.LightParameter.GL_DIFFUSE, pparams2);
                OpenGLImports.glLightf(lightName, OpenGLImports.LightSourceParameter.GL_SPOT_CUTOFF, (float)light.SpotAngle);
                OpenGLImports.glLightfv(lightName, OpenGLImports.LightParameter.GL_AMBIENT, pparams1);
                OpenGLImports.glLightfv(lightName, OpenGLImports.LightParameter.GL_SPECULAR, pparams3);
                OpenGLImports.glLightf(lightName, OpenGLImports.LightSourceParameter.GL_SPOT_EXPONENT, OpenGLImports.Spot_exponent);
                OpenGLImports.glLightf(lightName, OpenGLImports.LightSourceParameter.GL_CONSTANT_ATTENUATION, OpenGLImports.Attenuation_constant);
                OpenGLImports.glLightf(lightName, OpenGLImports.LightSourceParameter.GL_LINEAR_ATTENUATION, OpenGLImports.Attenuation_linear);
                OpenGLImports.glLightf(lightName, OpenGLImports.LightSourceParameter.GL_QUADRATIC_ATTENUATION, OpenGLImports.Attenuation_quadratic);
                if (light.Enable)
                    OpenGLImports.glEnable((OpenGLImports.GLCap)lightName);
                else
                    OpenGLImports.glDisable((OpenGLImports.GLCap)lightName);
            }
        }

        public Matrix OffsetMat
        {
            get => this.mOffsetMat;
            set => this.mOffsetMat = value;
        }

        public override gPoint OffsetPt => this.GetOffsetPt;

        private gPoint GetOffsetPt => !(this.GetOffsetMat != (Matrix)null) ? (gPoint)null : this.GetOffsetMat.Offset;

        private Matrix GetOffsetMat => this.mShared != null ? this.mShared.OffsetMat : this.OffsetMat;

        private void getCoordCorrection()
        {
            gPoint gPoint = new gPoint();
            Box box1 = new Box();
            if (this.DrawingExtents.IsNormal)
            {
                box1 = new Box(this.DrawingExtents);
                gPoint = box1.MidPoint;
            }
            if (this.mShared != null)
            {
                Box box2 = this.mShared.DrawingExtents;
                if (box2.IsEmpty)
                    box2 = (this.mShared.OwnerObject as IgrPrinterProperties).GetExtents();
                if (!box2.IsEmpty)
                    box1 = box2;
                gPoint = box1.MidPoint;
            }
            if (this.GetOffsetMat != (Matrix)null && box1.IsNormal && this.GetOffsetPt.Distance3D(gPoint) > 50000.0)
            {
                if (this.mShared != null)
                    this.mShared.OffsetMat = (Matrix)null;
                else
                    this.OffsetMat = (Matrix)null;
            }
            if (!(this.GetOffsetMat == (Matrix)null) || !box1.IsNormal || this.GlobalProperties.OpenglCoordCorrectionLimit != 0.0 && gPoint.Distance3D(new gPoint()) < this.GlobalProperties.OpenglCoordCorrectionLimit)
                return;
            if (this.mShared != null)
            {
                this.mShared.OffsetMat = new Matrix();
                this.mShared.OffsetMat.TranslateMatrix(gPoint);
            }
            else
            {
                this.OffsetMat = new Matrix();
                this.OffsetMat.TranslateMatrix(gPoint);
            }
        }

        public override bool EnableCoordCorrection(bool bvalue)
        {
            bool enableCoordCorrection = this.mEnableCoordCorrection;
            this.mEnableCoordCorrection = bvalue;
            return enableCoordCorrection;
        }

        public override bool IsEnableCoordCorrection => this.mEnableCoordCorrection;

        /// <summary>
        /// Pushes <see cref="P:vdShader.vdrawglRender_opengl_2.OffsetPt" /> used for lagre coordinate correction.
        /// </summary>
        /// <param name="useOpenGL">Set it to true just before calling <see cref="M:VectorDraw.Render.OpenGL.OpenGLImports.glCallList(System.UInt32)" /> .</param>
        public void PushOffsetMatrix(bool useOpenGL)
        {
            if (!(this.GetOffsetMat != (Matrix)null))
                return;
            if (useOpenGL)
            {
                OpenGLImports.glPushMatrix();
                OpenGLImports.glLoadMatrixd(OpenGLImports.GetGLMatrix(this.GetOffsetMat * this.CurrentMatrix));
            }
            else
                this.PushMatrix(this.GetOffsetMat);
            this.mEnableCoordCorrection = true;
        }

        /// <summary>
        /// Pop previous <see cref="M:vdShader.vdrawglRender_opengl_2.PushOffsetMatrix(System.Boolean)" />
        /// </summary>
        /// <param name="useOpenGL">Set it to true just after calling <see cref="M:VectorDraw.Render.OpenGL.OpenGLImports.glCallList(System.UInt32)" /> . </param>
        public void PopOffsetMatrix(bool useOpenGL)
        {
            if (!(this.GetOffsetMat != (Matrix)null))
                return;
            this.mEnableCoordCorrection = false;
            if (useOpenGL)
                OpenGLImports.glPopMatrix();
            else
                this.PopMatrix();
        }

        private void SelectModelMatrix(Matrix m)
        {
            if (!this.mIsLockGL)
                return;
            OpenGLImports.glMatrixMode(OpenGLImports.MatrixMode.GL_MODELVIEW);
            if (this.mEnableCoordCorrection && this.GetOffsetMat != (Matrix)null)
                OpenGLImports.glLoadMatrixd(OpenGLImports.GetGLMatrix(this.GetOffsetMat * m));
            else
                OpenGLImports.glLoadMatrixd(OpenGLImports.GetGLMatrix(m));
        }

        public override void StartDraw(bool updateproperties)
        {
            this.mEdgePass = vdrawglRender_opengl_2.EdgePassFlag.None;
            this.mShadowLightMode = vdRender.ShadowLightModeFlag.None;
            this.mContainsTrasparent = false;
            this.mTmpViewCenter = new gPoint(this.ViewCenter);
            this.mTmpCurrentMatrix = new Matrix(this.CurrentMatrix);
            Matrix matrix = new Matrix();
            matrix.Multiply(this.CurrentMatrix);
            matrix.TranslateMatrix(this.ViewCenter * -1.0);
            this.SetCurrentMatrix(matrix);
            this.ViewCenter = new gPoint();
            base.StartDraw(updateproperties);
            if (!this.Started)
                return;
            if (this.mIsLockGL)
            {
                for (int cap = 16384; cap <= 16391; ++cap)
                    OpenGLImports.glDisable((OpenGLImports.GLCap)cap);
            }
            this.PrepareRenderMode();
            this.getCoordCorrection();
        }

        public override void EndDraw()
        {
            base.EndDraw();
            if (!(this.mTmpCurrentMatrix != (Matrix)null))
                return;
            this.SetCurrentMatrix(this.mTmpCurrentMatrix);
            this.ViewCenter = this.mTmpViewCenter;
            this.mTmpViewCenter = (gPoint)null;
            this.mTmpCurrentMatrix = (Matrix)null;
        }

        public override void Update()
        {
            if (this.mShared != null)
            {
                base.Update();
            }
            else
            {
                this.ListItems.UpdateAllLists();
                base.Update();
                this.OffsetMat = (Matrix)null;
            }
        }

        private void TextImage2D(int strideWidth, int height, IntPtr bytes, bool IsImageBindMapped)
        {
            OpenGLImports.glTexParameteri(OpenGLImports.TargetTexture.GL_TEXTURE_2D, OpenGLImports.TextureParameterName.GL_TEXTURE_MIN_FILTER, OpenGLImports.TextureFilters.GL_LINEAR);
            OpenGLImports.glTexParameteri(OpenGLImports.TargetTexture.GL_TEXTURE_2D, OpenGLImports.TextureParameterName.GL_TEXTURE_MAG_FILTER, OpenGLImports.TextureFilters.GL_LINEAR);
            if (IsImageBindMapped)
                OpenGLImports.glTexParameterfv(OpenGLImports.TargetTexture.GL_TEXTURE_2D, OpenGLImports.TextureParameterName.GL_TEXTURE_BORDER_COLOR, new float[4]
                {
          1f,
          1f,
          1f,
          0.0f
                });
            else
                OpenGLImports.glTexParameterfv(OpenGLImports.TargetTexture.GL_TEXTURE_2D, OpenGLImports.TextureParameterName.GL_TEXTURE_BORDER_COLOR, new float[4]);
            OpenGLImports.glTexImage2D(OpenGLImports.TargetTexture.GL_TEXTURE_2D, 0, OpenGLImports.PixelFormats.GL_4, strideWidth / 4, height, 0, OpenGLImports.TexturePixelFormat.GL_BGRA_EXT, OpenGLImports.PixelType.GL_UNSIGNED_BYTE, bytes);
        }

        internal override void OnImageBindCreated(IntPtr image, int Flag)
        {
            if (!this.mIsLockGL)
            {
                base.OnImageBindCreated(image, Flag);
            }
            else
            {
                if (!(image != IntPtr.Zero) || !this.ListItems.mImageBinds.ContainsKey(image))
                    return;
                uint mImageBind = this.ListItems.mImageBinds[image];
                if (OpenGLImports.glIsTexture(mImageBind) == (byte)0)
                    return;
                OpenGLImports.glDeleteTextures(1, new uint[1]
                {
          mImageBind
                });
                --vdgl.nTextures;
            }
        }

        private uint calculateBind(IntPtr image, vdglTypes.MATERIAL_FLAG materialFlag)
        {
            uint texture = 0;
            bool IsImageBindMapped = image != IntPtr.Zero && (materialFlag & vdglTypes.MATERIAL_FLAG.MAPPEDIMAGE) != 0;
            if (image != IntPtr.Zero)
            {
                vdglTypes.IImageWrapper imageWrapper = vdgl.WrapImage(image);
                if (!imageWrapper.IsNull)
                {
                    bool flag = false;
                    if (this.ListItems.mImageBinds.ContainsKey(image))
                    {
                        texture = this.ListItems.mImageBinds[image];
                        if (OpenGLImports.glIsTexture(texture) != (byte)0)
                            flag = true;
                        else if (this.ActiveGLList == 0U && OpenGLImports.glIsTexture(texture) != (byte)0)
                            OpenGLImports.glDeleteTextures(1, new uint[1]
                            {
                texture
                            });
                    }
                    if (!flag && this.ActiveGLList == 0U)
                    {
                        uint[] textures = new uint[1];
                        OpenGLImports.glGenTextures(1, textures);
                        texture = textures[0];
                        OpenGLImports.glBindTexture(OpenGLImports.TargetTexture.GL_TEXTURE_2D, texture);
                        ++vdgl.nTextures;
                        if (!this.ListItems.mImageBinds.ContainsKey(image))
                            this.ListItems.mImageBinds.Add(image, texture);
                        else
                            this.ListItems.mImageBinds[image] = texture;
                        int cb = imageWrapper.stridewidth * imageWrapper.height;
                        int num1 = vdRenderGlobalProperties.ReduceScale(imageWrapper.width, imageWrapper.height, vdRenderGlobalProperties.mMaxBmpOpenGLImageSize, 32);
                        if (num1 > 1 || IsImageBindMapped && vdRenderGlobalProperties.OpenGLMappedImageBoundWidth > (byte)0)
                        {
                            System.Drawing.Imaging.PixelFormat defaultPixelFormat = BitmapWrapper.DefaultPixelFormat;
                            Bitmap bitmap1 = new Bitmap(imageWrapper.width, imageWrapper.height, defaultPixelFormat);
                            BitmapData bitmapdata1 = bitmap1.LockBits(new Rectangle(0, 0, bitmap1.Width, bitmap1.Height), ImageLockMode.ReadWrite, defaultPixelFormat);
                            imageWrapper.CopyTo(bitmapdata1.Scan0);
                            bitmap1.UnlockBits(bitmapdata1);
                            if (IsImageBindMapped && vdRenderGlobalProperties.OpenGLMappedImageBoundWidth > (byte)0)
                            {
                                int mappedImageBoundWidth = (int)vdRenderGlobalProperties.OpenGLMappedImageBoundWidth;
                                int num2 = bitmap1.Width / num1;
                                int num3 = bitmap1.Height / num1;
                                Bitmap bitmap2 = new Bitmap(num2 + mappedImageBoundWidth * 2, num3 + mappedImageBoundWidth * 2, defaultPixelFormat);
                                Graphics graphics = Graphics.FromImage((Image)bitmap2);
                                graphics.FillRectangle((Brush)new SolidBrush(Color.FromArgb(0, (int)byte.MaxValue, (int)byte.MaxValue, (int)byte.MaxValue)), new Rectangle(0, 0, bitmap2.Width, bitmap2.Height));
                                graphics.DrawImage((Image)bitmap1, mappedImageBoundWidth, mappedImageBoundWidth);
                                BitmapData bitmapdata2 = bitmap2.LockBits(new Rectangle(0, 0, bitmap2.Width, bitmap2.Height), ImageLockMode.ReadWrite, defaultPixelFormat);
                                try
                                {
                                    this.TextImage2D(bitmapdata2.Stride, bitmapdata2.Height, bitmapdata2.Scan0, IsImageBindMapped);
                                }
                                catch
                                {
                                }
                                bitmap2.UnlockBits(bitmapdata2);
                                bitmap2.Dispose();
                                bitmap1.Dispose();
                            }
                            else
                            {
                                Bitmap thumbnailImage = bitmap1.GetThumbnailImage(bitmap1.Width / num1, bitmap1.Height / num1, (Image.GetThumbnailImageAbort)null, IntPtr.Zero) as Bitmap;
                                BitmapData bitmapdata3 = thumbnailImage.LockBits(new Rectangle(0, 0, thumbnailImage.Width, thumbnailImage.Height), ImageLockMode.ReadWrite, defaultPixelFormat);
                                try
                                {
                                    this.TextImage2D(bitmapdata3.Stride, bitmapdata3.Height, bitmapdata3.Scan0, IsImageBindMapped);
                                }
                                catch
                                {
                                }
                                thumbnailImage.UnlockBits(bitmapdata3);
                                thumbnailImage.Dispose();
                            }
                        }
                        else
                        {
                            IntPtr num4 = Marshal.AllocCoTaskMem(cb);
                            imageWrapper.CopyTo(num4);
                            try
                            {
                                this.TextImage2D(imageWrapper.stridewidth, imageWrapper.height, num4, IsImageBindMapped);
                            }
                            catch
                            {
                            }
                            Marshal.FreeCoTaskMem(num4);
                        }
                    }
                }
            }
            return texture;
        }

        internal override void OnImageBind(IntPtr image, vdglTypes.MATERIAL_FLAG materialFlag)
        {
            if (!this.mIsLockGL)
            {
                base.OnImageBind(image, materialFlag);
            }
            else
            {
                if (this.mActiveBindImage == image)
                    return;
                bool flag = image != IntPtr.Zero && (materialFlag & vdglTypes.MATERIAL_FLAG.MAPPEDIMAGE) != 0;
                uint bind = this.calculateBind(image, materialFlag);
                this.mActiveBindImage = image;
                OpenGLImports.glBindTexture(OpenGLImports.TargetTexture.GL_TEXTURE_2D, bind);
                if (bind == 0U)
                    return;
                OpenGLImports.glTexParameteri(OpenGLImports.TargetTexture.GL_TEXTURE_2D, OpenGLImports.TextureParameterName.GL_TEXTURE_WRAP_S, flag ? OpenGLImports.TextureFilters.GL_CLAMP : OpenGLImports.TextureFilters.GL_REPEAT);
                OpenGLImports.glTexParameteri(OpenGLImports.TargetTexture.GL_TEXTURE_2D, OpenGLImports.TextureParameterName.GL_TEXTURE_WRAP_T, flag ? OpenGLImports.TextureFilters.GL_CLAMP : OpenGLImports.TextureFilters.GL_REPEAT);
            }
        }

        public override vdRender.PolygonModeEnum PolygonMode
        {
            get => base.PolygonMode;
            set
            {
                vdRender.PolygonModeEnum polygonMode = base.PolygonMode;
                base.PolygonMode = value;
                if (polygonMode != value)
                    this.FlushDrawBuffers(-1);
                if (!this.mIsLockGL)
                    return;
                vdRender.PolygonModeEnum polygonModeEnum = this.PolygonMode;
                if (polygonModeEnum == vdRender.PolygonModeEnum.DEFAULT)
                    polygonModeEnum = this.IsWire2d || this.RenderMode == vdRender.Mode.Wire3d ? vdRender.PolygonModeEnum.LINES : vdRender.PolygonModeEnum.FILL;
                if (polygonModeEnum == vdRender.PolygonModeEnum.LINES)
                    OpenGLImports.glPolygonMode(OpenGLImports.FaceMode.GL_FRONT_AND_BACK, OpenGLImports.PolygonMode.GL_LINE);
                else
                    OpenGLImports.glPolygonMode(OpenGLImports.FaceMode.GL_FRONT_AND_BACK, OpenGLImports.PolygonMode.GL_FILL);
            }
        }

        private uint ActiveGLList
        {
            get
            {
                if (!this.mIsLockGL)
                    return 0;
                int[] pparams = new int[1];
                OpenGLImports.glGetIntegerv(OpenGLImports.Parameters.GL_LIST_INDEX, pparams);
                return (uint)pparams[0];
            }
        }

        public override void SetTransparentOn()
        {
            base.SetTransparentOn();
            this.mContainsTrasparent = true;
        }

        internal override bool TestDrawBreak() => this.ActiveGLList <= 0U && base.TestDrawBreak();

        public override void TestTimerEvent()
        {
            if (this.ActiveGLList > 0U || this.mShadowLightMode == vdRender.ShadowLightModeFlag.CreateShadow)
                return;
            base.TestTimerEvent();
        }

        public override void DrawBindMappedImageList(
          IRenderListItem sender,
          ISupportdMappedImages polyface)
        {
            if (this.mEdgePass != vdrawglRender_opengl_2.EdgePassFlag.None || this.mShadowLightMode == vdRender.ShadowLightModeFlag.ApplyShadow || this.mShadowLightMode == vdRender.ShadowLightModeFlag.CreateShadow || polyface == null)
                return;
            int numMappedImages = polyface.GetNumMappedImages();
            if (numMappedImages == 0)
                return;
            this.FlushDrawBuffers(-1);
            if (!this.mIsLockGL)
            {
                base.DrawBindMappedImageList(sender, polyface);
                this.FlushDrawBuffers(-1);
            }
            else
            {
                bool flag1 = polyface is IPolyface && ((IPolyface)polyface).CanDrawMeshes((vdRender)this);
                IntPtr drawingList = sender.DrawingList;
                if (drawingList == IntPtr.Zero && !flag1 || this.IsCreatingList || this.ActiveGLList > 0U || this.IsSelectingMode || this.ActiveHighLightFilter == vdRender.HighLightFilter.On || this.RenderMode != vdRender.Mode.Shade && this.RenderMode != vdRender.Mode.ShadeOn && this.RenderMode != vdRender.Mode.Render && this.RenderMode != vdRender.Mode.RenderOn || this.IsDrawEdgeOn)
                    return;
                bool flag2 = vdRenderGlobalProperties.mOpenGLDoubleBuffered && (flag1 || (sender.Draw3DFlag & Draw3DFlagEnum.ExcludeFrom3DList) == Draw3DFlagEnum.Default);
                if (!flag1 & flag2 && this.ListItems.mgllists_Deleted.ContainsKey(drawingList))
                    return;
                gPoint gPoint = this.GetOffsetPt;
                if (flag1)
                    gPoint = ((IPolyface)polyface).MeshOffset;
                this.ClearTextureInternalLists();
                OpenGLImports.glPushAttrib(OpenGLImports.AttribMask.GL_DEPTH_BUFFER_BIT | OpenGLImports.AttribMask.GL_ENABLE_BIT | OpenGLImports.AttribMask.GL_TEXTURE_BIT);
                bool bvalue1 = this.EnableTexture(true);
                bool bvalue2 = this.EnableDepthBufferWrite(false);
                bool bvalue3 = this.EnableBufferId(false);
                OpenGLImports.glDepthFunc(OpenGLImports.ComparisonFunction.GL_EQUAL);
                OpenGLImports.glTexGeni(OpenGLImports.TextureCoordName.GL_S, OpenGLImports.TextureGenParameter.GL_TEXTURE_GEN_MODE, OpenGLImports.TextureGenMode.GL_OBJECT_LINEAR);
                OpenGLImports.glTexGeni(OpenGLImports.TextureCoordName.GL_T, OpenGLImports.TextureGenParameter.GL_TEXTURE_GEN_MODE, OpenGLImports.TextureGenMode.GL_OBJECT_LINEAR);
                OpenGLImports.glTexGeni(OpenGLImports.TextureCoordName.GL_R, OpenGLImports.TextureGenParameter.GL_TEXTURE_GEN_MODE, OpenGLImports.TextureGenMode.GL_OBJECT_LINEAR);
                OpenGLImports.glTexGeni(OpenGLImports.TextureCoordName.GL_Q, OpenGLImports.TextureGenParameter.GL_TEXTURE_GEN_MODE, OpenGLImports.TextureGenMode.GL_OBJECT_LINEAR);
                OpenGLImports.glEnable(OpenGLImports.GLCap.GL_COLOR_MATERIAL);
                if (flag2)
                {
                    OpenGLImports.glEnable(OpenGLImports.GLCap.GL_TEXTURE_GEN_S);
                    OpenGLImports.glEnable(OpenGLImports.GLCap.GL_TEXTURE_GEN_T);
                }
                OpenGLImports.glDisable(OpenGLImports.GLCap.GL_TEXTURE_GEN_R);
                OpenGLImports.glDisable(OpenGLImports.GLCap.GL_TEXTURE_GEN_Q);
                for (int index = 0; index < numMappedImages; ++index)
                {
                    IBindMappedImage mappedImageAt = polyface.GetMappedImageAt(index);
                    if (mappedImageAt != null && !mappedImageAt.Deleted && mappedImageAt.ImageBind != null && mappedImageAt.Visible && mappedImageAt.clipbound(sender as IBoundingBox))
                    {
                        if (!this.TestDrawBreak())
                        {
                            IntPtr bindPtr = mappedImageAt.GetBindPtr((vdRender)this);
                            this.OnImageBind(bindPtr, flag2 ? vdglTypes.MATERIAL_FLAG.MAPPEDIMAGE : vdglTypes.MATERIAL_FLAG.NONE);
                            if ((flag1 || this.ListItems.mImageBinds.ContainsKey(bindPtr)) && OpenGLImports.glIsTexture(this.ListItems.mImageBinds[bindPtr]) != (byte)0)
                            {
                                vdglTypes.IImageWrapper imageWrapper = vdgl.WrapImage(bindPtr);
                                if (!imageWrapper.IsNull)
                                {
                                    Matrix mat = new Matrix();
                                    double num1 = (double)vdRenderGlobalProperties.OpenGLMappedImageBoundWidth + 0.6;
                                    double width = (double)imageWrapper.width;
                                    double height = (double)imageWrapper.height;
                                    double num2 = num1 / width;
                                    double num3 = (width + num1 * 2.0) / (height + num1 * 2.0);
                                    mat.TranslateMatrix(-num2, -num2 * num3, 0.0);
                                    mat.ScaleMatrix(1.0 + num2 * 2.0, 1.0 + num2 * 2.0, 1.0);
                                    mat.ScaleMatrix(1.0, 1.0 / num3, 1.0);
                                    mat.Multiply(mappedImageAt.MaterialMatrix);
                                    if (gPoint != (gPoint)null)
                                        mat.TranslateMatrix(-gPoint.x, -gPoint.y, -gPoint.z);
                                    mat.Invert();
                                    if (!flag2)
                                    {
                                        double[] imageMatrix = vdgl.AsvdrawContextMatrix(mat);
                                        vdgl.SetMaterial(this.vdContext, (int)byte.MaxValue, (int)byte.MaxValue, (int)byte.MaxValue, (int)byte.MaxValue, vdglTypes.PenWidthFlag.PIXEL, 0.0, IntPtr.Zero, 1.0, bindPtr, imageMatrix, 0, (int)byte.MaxValue, (int)byte.MaxValue, (int)byte.MaxValue, vdglTypes.MATERIAL_FLAG.MAPPEDIMAGE | vdglTypes.MATERIAL_FLAG.PUSHED);
                                        vdglTypes.SetPropertyValue_delegate setPropertyValue1 = vdgl.SetPropertyValue;
                                        IntPtr vdContext1 = this.vdContext;
                                        double[] PropertyValue1 = new double[1];
                                        vdglTypes.PropertyValues propertyValues = vdglTypes.PropertyValues.POLYGON_MODE_LINES;
                                        PropertyValue1[0] = (double)propertyValues.GetHashCode();
                                        setPropertyValue1(vdContext1, vdglTypes.PropertyType.MATERIAL_LOCK, PropertyValue1);
                                        if (flag1)
                                        {
                                            int num4 = (int)((IPolyface)polyface).DrawMeshes((vdRender)this);
                                        }
                                        else
                                            this.DrawList(sender);
                                        vdglTypes.SetPropertyValue_delegate setPropertyValue2 = vdgl.SetPropertyValue;
                                        IntPtr vdContext2 = this.vdContext;
                                        double[] PropertyValue2 = new double[1];
                                        propertyValues = vdglTypes.PropertyValues.TEXTURE_OFF;
                                        PropertyValue2[0] = (double)propertyValues.GetHashCode();
                                        setPropertyValue2(vdContext2, vdglTypes.PropertyType.MATERIAL_LOCK, PropertyValue2);
                                        vdgl.SetMaterial(this.vdContext, (int)byte.MaxValue, (int)byte.MaxValue, (int)byte.MaxValue, (int)byte.MaxValue, vdglTypes.PenWidthFlag.PIXEL, 0.0, IntPtr.Zero, 1.0, IntPtr.Zero, imageMatrix, 0, (int)byte.MaxValue, (int)byte.MaxValue, (int)byte.MaxValue, vdglTypes.MATERIAL_FLAG.MAPPEDIMAGE | vdglTypes.MATERIAL_FLAG.POPED);
                                    }
                                    else
                                    {
                                        OpenGLImports.glTexGendv(OpenGLImports.TextureCoordName.GL_S, OpenGLImports.TextureGenParameter.GL_OBJECT_PLANE, new double[4]
                                        {
                      mat.A00,
                      mat.A01,
                      mat.A02,
                      mat.A03
                                        });
                                        OpenGLImports.glTexGendv(OpenGLImports.TextureCoordName.GL_T, OpenGLImports.TextureGenParameter.GL_OBJECT_PLANE, new double[4]
                                        {
                      mat.A10,
                      mat.A11,
                      mat.A12,
                      mat.A13
                                        });
                                        if (flag1)
                                        {
                                            int num5 = (int)((IPolyface)polyface).DrawMeshes((vdRender)this);
                                        }
                                        else
                                            this.DrawList(sender);
                                    }
                                }
                            }
                        }
                        else
                            break;
                    }
                }
                this.FlushDrawBuffers(-1);
                this.EnableDepthBufferWrite(bvalue2);
                this.EnableTexture(bvalue1);
                this.EnableBufferId(bvalue3);
                OpenGLImports.glPopAttrib();
                this.UpdateListColors();
                this.OnImageBind(IntPtr.Zero, vdglTypes.MATERIAL_FLAG.NONE);
            }
        }

        public void SetColor(Color color)
        {
            if (vdRender.IsColorEmpty(color))
                return;
            this.SetColor(new byte[4]
            {
        color.R,
        color.G,
        color.B,
        color.A
            }, vdglTypes.FLAG_ELEMENT.None);
        }

        private void SetColor(byte[] color, vdglTypes.FLAG_ELEMENT eflag)
        {
            if (color == null)
                return;
            OpenGLImports.glColor4ubv(color);
            OpenGLImports.glMaterialfv(OpenGLImports.FaceMode.GL_FRONT_AND_BACK, OpenGLImports.MaterialParameter.GL_AMBIENT_AND_DIFFUSE, new float[4]
            {
        (float) color[0] / (float) byte.MaxValue,
        (float) color[1] / (float) byte.MaxValue,
        (float) color[2] / (float) byte.MaxValue,
        (float) color[3] / (float) byte.MaxValue
            });
            this.mContainsTrasparent = this.mContainsTrasparent || color[3] != byte.MaxValue && color[3] > (byte)0;
        }

        public override void PenStyleChanged(vdGdiPenStyle previousPen)
        {
            base.PenStyleChanged(previousPen);
            if (!this.mIsLockGL || this.IsCreatingList || this.ActiveGLList > 0U)
                return;
            if (previousPen != (vdGdiPenStyle)null && ((previousPen.ByBlockProperties ^ this.PenStyle.ByBlockProperties) & vdGdiPenStyle.ByblockTypeEnum.Color) != vdGdiPenStyle.ByblockTypeEnum.None)
                this.FlushDrawBuffers(-1);
            this.SetColor(this.SystemPenColor);
        }

        public override void MatrixViewChanged()
        {
            base.MatrixViewChanged();
            if (this.IsCreatingList || this.ActiveGLList > 0U)
                return;
            this.SelectModelMatrix(this.CurrentMatrix);
        }

        public void Vertex3d(gPoint pt) => this.Vertex3d(pt.x, pt.y, pt.z);

        public void SetNormal(Vector normal) => this.SetNormal(normal.x, normal.y, normal.z);

        public void SetNormal(double x, double y, double z) => OpenGLImports.glNormal3d(x, y, z);

        public double SetLineWidth(double width)
        {
            double pixelWidth = (double)this.PenStyle.GetPixelWidth((vdRender)this);
            if (!Globals.AreEqual(pixelWidth, width, 0.001))
            {
                this.PenStyle.SetLwWidth(width * 100.0 * 25.4 / (double)this.DpiY, (DoubleArray)null);
                this.PenStyleChanged((vdGdiPenStyle)null);
                if (this.mIsLockGL)
                    OpenGLImports.glLineWidth((float)Math.Max(this.mLineWidthMinimumDefault, width));
            }
            return pixelWidth;
        }

        /// <summary>Set the visibilty of Edge lines</summary>
        /// <param name="flag">Set to 1 for visible edge lines or 0 for invisible.</param>
        public void SetEdgeFlag(byte flag) => OpenGLImports.glEdgeFlag(flag);

        public void DrawPrimitiveBegin(OpenGLImports.Primitives type) => OpenGLImports.glBegin(type);

        public void DrawPrimitiveEnd() => OpenGLImports.glEnd();

        /// <summary>
        /// overrides the <see cref="M:VectorDraw.Render.vdRender.CloseOpenPolygon" />
        /// </summary>
        public override void CloseOpenPolygon()
        {
        }

        public void Vertex3d(double x, double y, double z)
        {
            if (this.mEnableCoordCorrection && this.GetOffsetMat != (Matrix)null)
            {
                gPoint getOffsetPt = this.GetOffsetPt;
                OpenGLImports.glVertex3d(x - getOffsetPt.x, y - getOffsetPt.y, z - getOffsetPt.z);
            }
            else
                OpenGLImports.glVertex3d(x, y, z);
        }

        public override void ClearDisplayLists(bool bFinalized)
        {
            if (this.mShared == null && !bFinalized && !this.mDestoyed)
            {
                IopenglControl openGlControlObject = this.OpenGLControlObject;
                if (openGlControlObject != null)
                {
                    lock (openGlControlObject)
                    {
                        bool flag = openGlControlObject.IsLock();
                        if (!flag)
                            openGlControlObject.LockOpenGLContect();
                        try
                        {
                            this.ListItems.ClearAllLists(openGlControlObject);
                            this.ClearShadowResources();
                        }
                        catch
                        {
                        }
                        finally
                        {
                            if (!flag)
                                openGlControlObject.UnLockOpenGLContect();
                        }
                    }
                }
            }
            base.ClearDisplayLists(bFinalized);
        }

        public override void ListDeleted(IRenderListItem fig)
        {
            IntPtr drawingList = fig.DrawingList;
            if (this.ListItems.mExcludeFromList.ContainsKey(fig))
                this.ListItems.mExcludeFromList.Remove(fig);
            if (drawingList == IntPtr.Zero || this.ListItems.mgllists_Deleted.ContainsKey(drawingList))
                return;
            uint num = 0;
            this.ListItems.mgllists_Deleted.Add(drawingList, num);
        }

        public override void EndList(IRenderListItem listItem)
        {
            if (this.mIsLockGL)
            {
                if (this.ActiveGLList != 0U)
                    return;
                if (listItem.SupportOpenGLPrimitives((IvdOpenGLRender)this))
                {
                    uint num;
                    if (this.ListItems.mSub_gllists.ContainsKey(listItem))
                    {
                        num = this.ListItems.mSub_gllists[listItem];
                    }
                    else
                    {
                        num = OpenGLImports.glGenLists(1);
                        ++vdgl.nLists;
                        this.ListItems.mSub_gllists.Add(listItem, num);
                    }
                    OpenGLImports.glNewList(num, OpenGLImports.ListMode.GL_COMPILE);
                    OpenGLImports.glPushAttrib(OpenGLImports.AttribMask.GL_COLOR_BUFFER_BIT);
                    bool containsTrasparent = this.mContainsTrasparent;
                    this.mContainsTrasparent = false;
                    listItem.OnDrawOpenGLPrimitives((IvdOpenGLRender)this);
                    uint transparency = this.mContainsTrasparent ? 1U : 0U;
                    this.mContainsTrasparent = containsTrasparent;
                    OpenGLImports.glPopAttrib();
                    OpenGLImports.glEndList();
                    vdgl.AddOpenGLListId(this.vdContext, num, transparency);
                }
            }
            base.EndList(listItem);
        }

        public override int DrawList(IRenderListItem listItem)
        {
            if (listItem == null)
                return -1;
            IntPtr drawingList = listItem.DrawingList;
            if (drawingList == IntPtr.Zero)
                return -1;
            if (this.mEdgePass != vdrawglRender_opengl_2.EdgePassFlag.None && this.GlobalProperties.EdgeEffect != vdRenderGlobalProperties.EdgeEffectFlag.DashedHidden || !vdRenderGlobalProperties.mOpenGLDoubleBuffered || (listItem.Draw3DFlag & Draw3DFlagEnum.ExcludeFrom3DList) != Draw3DFlagEnum.Default)
                return base.DrawList(listItem);
            if (!this.mIsLockGL || this.IsCreatingList || this.ActiveGLList > 0U)
                return base.DrawList(listItem);
            if (this.LockPenStyle != (vdGdiPenStyle)null && !this.IsCreatingList && this.ActiveGLList == 0U)
                return base.DrawList(listItem);
            glListItems listItems = this.ListItems;
            if (listItems.mExcludeFromList.ContainsKey(listItem))
                return base.DrawList(listItem);
            bool flag1 = listItem.SupportOpenGLPrimitives((IvdOpenGLRender)this);
            bool flag2 = listItems.mSub_gllists.ContainsKey(listItem);
            if (flag1 && !flag2)
                return -1;
            uint list = 0;
            if (listItems.mgllists_Deleted.ContainsKey(drawingList))
            {
                if (listItems.mgllists.ContainsKey(drawingList))
                    list = listItems.mgllists[drawingList];
                listItems.mgllists_Deleted.Remove(drawingList);
                listItems.mgllists.Remove(drawingList);
                if (listItems.mgllists_WithTransparent.ContainsKey(drawingList))
                    listItems.mgllists_WithTransparent.Remove(drawingList);
            }
            else if (listItems.mgllists.ContainsKey(drawingList))
            {
                uint mgllist = listItems.mgllists[drawingList];
                if (!this.IsBlendingOn || this.ListItems.mgllists_WithTransparent.ContainsKey(drawingList))
                {
                    this.PushOffsetMatrix(true);
                    this.SaveGLState();
                    OpenGLImports.glCallList(mgllist);
                    this.RestoreGLState();
                    this.PopOffsetMatrix(true);
                }
                if (this.ListItems.mgllists_WithTransparent.ContainsKey(drawingList))
                    vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.HAS_TRANSPARENT, new double[1]
                    {
            1.0
                    });
                if (!this.TestDrawBreak())
                    this.TestTimerEvent();
                return 0;
            }
            vdglTypes.ListStatus listStatus = vdgl.GetListStatus(this.vdContext, drawingList);
            if ((listStatus & (vdglTypes.ListStatus.DPI | vdglTypes.ListStatus.ALIGNTOVIEW)) != vdglTypes.ListStatus.NONE || !this.GlobalProperties.StrechText && (listStatus & vdglTypes.ListStatus.ALIGNTOVIEW__STRECHTEXT) != vdglTypes.ListStatus.NONE)
            {
                listItems.mExcludeFromList.Add(listItem, 0U);
                return base.DrawList(listItem);
            }
            this.FlushDrawBuffers(-1);
            this.PushToViewMatrix();
            double PropertyValue1 = 0.0;
            vdgl.GetPropertyValue(this.vdContext, vdglTypes.PropertyTypeGet.TRANSPARENT_ORDER, ref PropertyValue1);
            double PropertyValue2 = 0.0;
            vdgl.GetPropertyValue(this.vdContext, vdglTypes.PropertyTypeGet.HAS_TRANSPARENT, ref PropertyValue2);
            double PropertyValue3 = 0.0;
            vdgl.GetPropertyValue(this.vdContext, vdglTypes.PropertyTypeGet.POLYGONMODE, ref PropertyValue3);
            double PropertyValue4 = 0.0;
            vdgl.GetPropertyValue(this.vdContext, vdglTypes.PropertyTypeGet.DRAWEDGE_MODE, ref PropertyValue4);
            double PropertyValue5 = 0.0;
            vdgl.GetPropertyValue(this.vdContext, vdglTypes.PropertyTypeGet.ENABLE_TEXTURE, ref PropertyValue5);
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.TRANSPARENT_ORDER, new double[1]
            {
        (double) vdglTypes.PropertyValues.TEXTURE_OFF.GetHashCode()
            });
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.HAS_TRANSPARENT, new double[1]);
            vdglTypes.SetPropertyValue_delegate setPropertyValue1 = vdgl.SetPropertyValue;
            IntPtr vdContext1 = this.vdContext;
            double[] PropertyValue6 = new double[1];
            vdglTypes.PropertyValues propertyValues = vdglTypes.PropertyValues.TEXTURE_OFF;
            PropertyValue6[0] = (double)propertyValues.GetHashCode();
            setPropertyValue1(vdContext1, vdglTypes.PropertyType.DRAWEDGE_MODE, PropertyValue6);
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.POLYGONMODE, new double[1]
            {
        (double) vdRender.PolygonModeEnum.FILL.GetHashCode()
            });
            vdglTypes.SetPropertyValue_delegate setPropertyValue2 = vdgl.SetPropertyValue;
            IntPtr vdContext2 = this.vdContext;
            double[] PropertyValue7 = new double[1];
            propertyValues = vdglTypes.PropertyValues.POLYGON_MODE_LINES;
            PropertyValue7[0] = (double)propertyValues.GetHashCode();
            setPropertyValue2(vdContext2, vdglTypes.PropertyType.ENABLE_TEXTURE, PropertyValue7);
            vdGdiPenStyle newstyle = new vdGdiPenStyle();
            newstyle.SetLwWidth(0.0, (DoubleArray)null);
            newstyle.ByBlockProperties = vdGdiPenStyle.ByblockTypeEnum.All;
            this.PushPenstyle(newstyle);
            this.OnImageBind(IntPtr.Zero, vdglTypes.MATERIAL_FLAG.NONE);
            this.SaveGLState();
            if (OpenGLImports.glIsList(list) == (byte)0)
            {
                list = OpenGLImports.glGenLists(1);
                ++vdgl.nLists;
            }
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.IS_GL_LIST, new double[1]
            {
        1.0
            });
            OpenGLImports.glNewList(list, OpenGLImports.ListMode.GL_COMPILE);
            if (this.GetOffsetMat != (Matrix)null)
                this.PushMatrix(this.GetOffsetMat.GetInvertion());
            base.DrawList(listItem);
            this.FlushDrawBuffers(-1);
            if (this.GetOffsetMat != (Matrix)null)
                this.PopMatrix();
            OpenGLImports.glEndList();
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.IS_GL_LIST, new double[1]);
            this.RestoreGLState();
            double PropertyValue8 = 0.0;
            vdgl.GetPropertyValue(this.vdContext, vdglTypes.PropertyTypeGet.HAS_TRANSPARENT, ref PropertyValue8);
            this.PopPenstyle();
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.TRANSPARENT_ORDER, new double[1]
            {
        PropertyValue1
            });
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.HAS_TRANSPARENT, new double[1]
            {
        PropertyValue2
            });
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.POLYGONMODE, new double[1]
            {
        PropertyValue3
            });
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.DRAWEDGE_MODE, new double[1]
            {
        PropertyValue4
            });
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.ENABLE_TEXTURE, new double[1]
            {
        PropertyValue5
            });
            this.OnImageBind(IntPtr.Zero, vdglTypes.MATERIAL_FLAG.NONE);
            this.SetEdgeOnDefaultPenStyle();
            this.PopMatrix();
            listItems.mgllists.Add(drawingList, list);
            if (((int)PropertyValue8 & 1) != 0)
            {
                vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.HAS_TRANSPARENT, new double[1]
                {
          1.0
                });
                if (!listItems.mgllists_WithTransparent.ContainsKey(drawingList))
                    listItems.mgllists_WithTransparent.Add(drawingList, list);
            }
            return this.DrawList(listItem);
        }

        private void SaveGLState() => OpenGLImports.glGetBooleanv(OpenGLImports.Parameters.GL_COLOR_WRITEMASK, this.colormask);

        private void RestoreGLState() => OpenGLImports.glColorMask(this.colormask[0], this.colormask[1], this.colormask[2], this.colormask[3]);

        private void PureDrawArrays(vdglTypes.IBufferWrapper drawbuffer)
        {
            vdglTypes.IBufferWrapper bufferWrapper = drawbuffer;
            if (drawbuffer.type == 1)
            {
                int num = 8;
                if (drawbuffer.colorflag == vdglTypes.Colorflag.Truecolor && drawbuffer.containscolors != 1)
                    OpenGLImports.glColor4d(bufferWrapper[4], bufferWrapper[5], bufferWrapper[6], bufferWrapper[7]);
                if (drawbuffer.width > 0.0)
                    OpenGLImports.glLineWidth((float)Math.Max(this.mLineWidthMinimumDefault, drawbuffer.width));
                else if (drawbuffer.width == 0.0)
                    OpenGLImports.glCallList(this.ListItems.linewidth0);
                OpenGLImports.glDisable(OpenGLImports.GLCap.GL_LIGHTING);
                OpenGLImports.glCallList(this.ListItems.texturedisablelist);
                OpenGLImports.glCallList(this.ListItems.edgeonInvisible);
                OpenGLImports.glCallList(this.ListItems.hideshaonlist);
                OpenGLImports.glBegin((OpenGLImports.Primitives)drawbuffer.type);
                for (int index = 0; index < drawbuffer.curitem; index += num)
                {
                    if (drawbuffer.containscolors == 1)
                        OpenGLImports.glColor4d(bufferWrapper[index + 4], bufferWrapper[index + 5], bufferWrapper[index + 6], bufferWrapper[index + 7]);
                    OpenGLImports.glVertex3d(bufferWrapper[index], bufferWrapper[index + 1], bufferWrapper[index + 2]);
                }
                OpenGLImports.glEnd();
            }
            else if (drawbuffer.type == 0)
            {
                int num = 8;
                if (drawbuffer.colorflag == vdglTypes.Colorflag.Truecolor && drawbuffer.containscolors != 1)
                    OpenGLImports.glColor4d(bufferWrapper[4], bufferWrapper[5], bufferWrapper[6], bufferWrapper[7]);
                if (drawbuffer.width >= 0.0)
                    OpenGLImports.glPointSize((float)Math.Max(this.mLineWidthMinimumDefault, drawbuffer.width));
                OpenGLImports.glDisable(OpenGLImports.GLCap.GL_LIGHTING);
                OpenGLImports.glCallList(this.ListItems.texturedisablelist);
                OpenGLImports.glCallList(this.ListItems.edgeonInvisible);
                OpenGLImports.glCallList(this.ListItems.hideshaonlist);
                OpenGLImports.glBegin((OpenGLImports.Primitives)drawbuffer.type);
                for (int index = 0; index < drawbuffer.curitem; index += num)
                {
                    if (drawbuffer.containscolors == 1)
                        OpenGLImports.glColor4d(bufferWrapper[index + 4], bufferWrapper[index + 5], bufferWrapper[index + 6], bufferWrapper[index + 7]);
                    OpenGLImports.glVertex3d(bufferWrapper[index], bufferWrapper[index + 1], bufferWrapper[index + 2]);
                }
                OpenGLImports.glEnd();
            }
            else
            {
                int num = 16;
                if (drawbuffer.colorflag == vdglTypes.Colorflag.Truecolor && drawbuffer.containscolors != 1)
                    OpenGLImports.glColor4d(bufferWrapper[4], bufferWrapper[5], bufferWrapper[6], bufferWrapper[7]);
                if ((drawbuffer.drawFlag & vdglTypes.drawBufferFlag.ForceDisableLight) != vdglTypes.drawBufferFlag.None)
                    OpenGLImports.glDisable(OpenGLImports.GLCap.GL_LIGHTING);
                OpenGLImports.glCallList(this.ListItems.texture2dlist);
                if ((drawbuffer.drawFlag & vdglTypes.drawBufferFlag.ForceDisableTexture) != vdglTypes.drawBufferFlag.None)
                {
                    this.OnImageBind(IntPtr.Zero, vdglTypes.MATERIAL_FLAG.NONE);
                    OpenGLImports.glCallList(this.ListItems.texturedisablelist);
                }
                else if (drawbuffer.curimage.ToIntPtr != IntPtr.Zero)
                {
                    this.OnImageBind(drawbuffer.curimage.ToIntPtr, (vdglTypes.MATERIAL_FLAG)drawbuffer.imageflag);
                    OpenGLImports.glMaterialfv(OpenGLImports.FaceMode.GL_FRONT_AND_BACK, OpenGLImports.MaterialParameter.GL_AMBIENT_AND_DIFFUSE, new float[4]
                    {
            1f,
            1f,
            1f,
            (float) bufferWrapper[7]
                    });
                    if ((drawbuffer.drawFlag & vdglTypes.drawBufferFlag.ForceEnableTexture) != vdglTypes.drawBufferFlag.None)
                        OpenGLImports.glEnable(OpenGLImports.GLCap.GL_TEXTURE_2D);
                }
                else
                {
                    this.OnImageBind(IntPtr.Zero, vdglTypes.MATERIAL_FLAG.NONE);
                    OpenGLImports.glCallList(this.ListItems.texturedisablelist);
                }
                if ((drawbuffer.drawFlag & vdglTypes.drawBufferFlag.IsFillOn) != vdglTypes.drawBufferFlag.None)
                {
                    OpenGLImports.glCallList(this.ListItems.edgeonVisible);
                    OpenGLImports.glCallList(this.ListItems.edgeonColor);
                    OpenGLImports.glCallList(this.ListItems.edgeonWidth);
                }
                OpenGLImports.glCallList(this.ListItems.ShadowTextureBind);
                if ((drawbuffer.drawFlag & vdglTypes.drawBufferFlag.ForceDisableColorMaskOnHide) != vdglTypes.drawBufferFlag.None)
                    OpenGLImports.glCallList(this.ListItems.forceDisableColorOnHide);
                else
                    OpenGLImports.glCallList(this.ListItems.hideshaonlist);
                if ((drawbuffer.drawFlag & vdglTypes.drawBufferFlag.ForceEnableFill) != vdglTypes.drawBufferFlag.None)
                    OpenGLImports.glCallList(this.ListItems.solidforcevisibility);
                OpenGLImports.glBegin((OpenGLImports.Primitives)drawbuffer.type);
                for (int index = 0; index < drawbuffer.curitem; index += num)
                {
                    if (drawbuffer.containscolors == 1)
                        OpenGLImports.glColor4d(bufferWrapper[index + 4], bufferWrapper[index + 5], bufferWrapper[index + 6], bufferWrapper[index + 7]);
                    OpenGLImports.glNormal3d(bufferWrapper[index + 8], bufferWrapper[index + 9], bufferWrapper[index + 10]);
                    if (drawbuffer.curimage.ToIntPtr != IntPtr.Zero)
                        OpenGLImports.glTexCoord4d(bufferWrapper[index + 12], bufferWrapper[index + 13], bufferWrapper[index + 14], bufferWrapper[index + 15]);
                    OpenGLImports.glEdgeFlag((byte)bufferWrapper[index + 11]);
                    OpenGLImports.glVertex3d(bufferWrapper[index], bufferWrapper[index + 1], bufferWrapper[index + 2]);
                }
                OpenGLImports.glEnd();
                OpenGLImports.glEdgeFlag((byte)0);
            }
        }

        internal override void OnDrawMesh(
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
            OpenGLImports.glPushAttrib(this.glattribmask);
            Color color = this.SystemPenColor;
            if (this.IsDrawEdgeOn && !this.IsEdgeColorEmpty)
                color = this.EdgeColor;
            OpenGLImports.glColor4ubv(new byte[4]
            {
        color.R,
        color.G,
        color.B,
        color.A
            });
            OpenGLImports.glEnableClientState(OpenGLImports.Arrays.GL_VERTEX_ARRAY);
            OpenGLImports.glEnableClientState(OpenGLImports.Arrays.GL_NORMAL_ARRAY);
            if (colors != IntPtr.Zero)
                OpenGLImports.glEnableClientState(OpenGLImports.Arrays.GL_COLOR_ARRAY);
            if (edges != IntPtr.Zero)
                OpenGLImports.glEnableClientState(OpenGLImports.Arrays.GL_EDGE_FLAG_ARRAY);
            OpenGLImports.glVertexPointer(3, OpenGLImports.PixelType.GL_DOUBLE, 0, mesh_verts);
            OpenGLImports.glNormalPointer(OpenGLImports.PixelType.GL_DOUBLE, 0, mesh_normals);
            if (colors != IntPtr.Zero)
                OpenGLImports.glColorPointer(4, OpenGLImports.PixelType.GL_DOUBLE, 0, colors);
            if (edges != IntPtr.Zero)
                OpenGLImports.glEdgeFlagPointer(0, edges);
            bool flag = midpoint != null && (midpoint[0] != 0.0 || midpoint[1] != 0.0 || midpoint[2] != 0.0);
            if (flag)
                this.PushOffsetMatrix(true);
            OpenGLImports.glEnable(OpenGLImports.GLCap.GL_COLOR_MATERIAL);
            OpenGLImports.glEdgeFlag((byte)1);
            OpenGLImports.glDrawArrays(mesh_stride == (byte)4 ? OpenGLImports.Primitives.GL_QUADS : OpenGLImports.Primitives.GL_TRIANGLES, 0, mesh_items);
            OpenGLImports.glEdgeFlag((byte)0);
            if (flag)
                this.PopOffsetMatrix(true);
            OpenGLImports.glDisableClientState(OpenGLImports.Arrays.GL_VERTEX_ARRAY);
            OpenGLImports.glDisableClientState(OpenGLImports.Arrays.GL_NORMAL_ARRAY);
            OpenGLImports.glDisableClientState(OpenGLImports.Arrays.GL_COLOR_ARRAY);
            OpenGLImports.glDisableClientState(OpenGLImports.Arrays.GL_EDGE_FLAG_ARRAY);
            OpenGLImports.glDisableClientState(OpenGLImports.Arrays.GL_TEXTURE_COORD_ARRAY);
            OpenGLImports.glPopAttrib();
        }

        internal override void OnDrawArrays(IntPtr vdcontext, IntPtr drawbufferPtr)
        {
            vdglTypes.IBufferWrapper drawbuffer = vdgl.WrapBuffer(drawbufferPtr);
            OpenGLImports.AttribMask glattribmask = this.glattribmask;
            if (this.ActiveGLList == 0U)
            {
                glattribmask |= this.glattribmask2;
                OpenGLImports.glMatrixMode(OpenGLImports.MatrixMode.GL_MODELVIEW);
                OpenGLImports.glPushMatrix();
                OpenGLImports.glLoadIdentity();
            }
            OpenGLImports.glPushAttrib(glattribmask);
            if (!this.SketchPureDrawArrays(drawbuffer))
            {
                if (drawbuffer.enableactiveid != 0)
                    OpenGLImports.glStencilFunc(OpenGLImports.ComparisonFunction.GL_ALWAYS, drawbuffer.activeid, uint.MaxValue);
                if (drawbuffer.colorflag == vdglTypes.Colorflag.Background)
                    OpenGLImports.glCallList(this.ListItems.bkcolorlist);
                else if (drawbuffer.colorflag == vdglTypes.Colorflag.Forground)
                    OpenGLImports.glCallList(this.ListItems.forcolorlist);
                this.PureDrawArrays(drawbuffer);
            }
            OpenGLImports.glPopAttrib();
            if (this.ActiveGLList == 0U)
                OpenGLImports.glPopMatrix();
            drawbuffer.Free();
        }

        private void ClearTextureInternalLists()
        {
            if (this.ListItems.texture2dlist == 0U)
                this.ListItems.texture2dlist = OpenGLImports.glGenLists(1);
            OpenGLImports.glNewList(this.ListItems.texture2dlist, OpenGLImports.ListMode.GL_COMPILE);
            OpenGLImports.glEndList();
            if (this.ListItems.texturedisablelist == 0U)
                this.ListItems.texturedisablelist = OpenGLImports.glGenLists(1);
            OpenGLImports.glNewList(this.ListItems.texturedisablelist, OpenGLImports.ListMode.GL_COMPILE);
            OpenGLImports.glEndList();
            if (this.ListItems.ShadowTextureBind == 0U)
                this.ListItems.ShadowTextureBind = OpenGLImports.glGenLists(1);
            OpenGLImports.glNewList(this.ListItems.ShadowTextureBind, OpenGLImports.ListMode.GL_COMPILE);
            OpenGLImports.glEndList();
        }

        public override vdRender.Mode RenderMode
        {
            get => base.RenderMode;
            set
            {
                bool flag = this.RenderMode != value;
                base.RenderMode = value;
                if (!flag)
                    return;
                this.UpdateListRenderModeDepend();
            }
        }

        private void PuseSetColorMask(bool bvalue)
        {
            if (this.ListItems.hideshaonlist == 0U)
                this.ListItems.hideshaonlist = OpenGLImports.glGenLists(1);
            OpenGLImports.glNewList(this.ListItems.hideshaonlist, OpenGLImports.ListMode.GL_COMPILE);
            if (!bvalue && this.SupportEdgeRender && !this.IsDrawEdgeOn && this.RenderMode == vdRender.Mode.Hide)
                OpenGLImports.glColorMask((byte)0, (byte)0, (byte)0, (byte)0);
            else if (!bvalue)
                OpenGLImports.glColorMask((byte)0, (byte)0, (byte)0, (byte)0);
            else
                OpenGLImports.glColorMask((byte)1, (byte)1, (byte)1, this.GlobalProperties.IgnoreTransparency ? (byte)0 : (byte)1);
            OpenGLImports.glEndList();
            OpenGLImports.glCallList(this.ListItems.hideshaonlist);
        }

        private void FinishDrawScene()
        {
        }

        private void UpdateEdgeOnLists()
        {
            if (this.ListItems.solidforcevisibility == 0U)
                this.ListItems.solidforcevisibility = OpenGLImports.glGenLists(1);
            OpenGLImports.glNewList(this.ListItems.solidforcevisibility, OpenGLImports.ListMode.GL_COMPILE);
            if (this.RenderMode == vdRender.Mode.Wire3d || this.SupportEdgeRender && this.IsDrawEdgeOn && this.RenderMode == vdRender.Mode.Hide)
                OpenGLImports.glPolygonMode(OpenGLImports.FaceMode.GL_FRONT_AND_BACK, OpenGLImports.PolygonMode.GL_FILL);
            OpenGLImports.glEndList();
            if (this.ListItems.edgeonInvisible == 0U)
                this.ListItems.edgeonInvisible = OpenGLImports.glGenLists(1);
            OpenGLImports.glNewList(this.ListItems.edgeonInvisible, OpenGLImports.ListMode.GL_COMPILE);
            if (this.SupportEdgeRender && this.IsDrawEdgeOn && this.RenderMode != vdRender.Mode.Hide)
                OpenGLImports.glColorMask((byte)0, (byte)0, (byte)0, (byte)0);
            OpenGLImports.glEndList();
            if (this.ListItems.edgeonVisible == 0U)
                this.ListItems.edgeonVisible = OpenGLImports.glGenLists(1);
            OpenGLImports.glNewList(this.ListItems.edgeonVisible, OpenGLImports.ListMode.GL_COMPILE);
            if (this.SupportEdgeRender && this.IsDrawEdgeOn)
            {
                OpenGLImports.glPolygonMode(OpenGLImports.FaceMode.GL_BACK, OpenGLImports.PolygonMode.GL_LINE);
                OpenGLImports.glBindTexture(OpenGLImports.TargetTexture.GL_TEXTURE_2D, 0U);
            }
            OpenGLImports.glEndList();
            if (this.ListItems.edgeonColor == 0U)
                this.ListItems.edgeonColor = OpenGLImports.glGenLists(1);
            OpenGLImports.glNewList(this.ListItems.edgeonColor, OpenGLImports.ListMode.GL_COMPILE);
            if (this.SupportEdgeRender && this.IsDrawEdgeOn && !this.IsEdgeColorEmpty)
            {
                byte[] v = new byte[4]
                {
          this.EdgeColor.R,
          this.EdgeColor.G,
          (byte) 0,
          (byte) 0
                };
                Color edgeColor = this.EdgeColor;
                v[2] = edgeColor.B;
                edgeColor = this.EdgeColor;
                v[3] = edgeColor.A;
                OpenGLImports.glColor4ubv(v);
            }
            OpenGLImports.glEndList();
            if (this.ListItems.edgeonWidth == 0U)
                this.ListItems.edgeonWidth = OpenGLImports.glGenLists(1);
            OpenGLImports.glNewList(this.ListItems.edgeonWidth, OpenGLImports.ListMode.GL_COMPILE);
            if (this.SupportEdgeRender && this.IsDrawEdgeOn)
                OpenGLImports.glLineWidth(this.DpiY * this.GlobalProperties.EdgePenWidth);
            OpenGLImports.glEndList();
            if (this.ListItems.linewidth0 == 0U)
                this.ListItems.linewidth0 = OpenGLImports.glGenLists(1);
            OpenGLImports.glNewList(this.ListItems.linewidth0, OpenGLImports.ListMode.GL_COMPILE);
            double val2 = 0.0;
            if (this.IsPrinting || (double)this.GlobalProperties.MinPenWidth < 0.0)
                val2 = (double)this.DpiY * (double)Math.Abs(this.GlobalProperties.MinPenWidth);
            OpenGLImports.glLineWidth((float)Math.Max(this.mLineWidthMinimumDefault, val2));
            OpenGLImports.glEndList();
            if (this.ListItems.forceDisableColorOnHide == 0U)
                this.ListItems.forceDisableColorOnHide = OpenGLImports.glGenLists(1);
            OpenGLImports.glNewList(this.ListItems.forceDisableColorOnHide, OpenGLImports.ListMode.GL_COMPILE);
            if (this.RenderMode == vdRender.Mode.Hide && this.SupportEdgeRender && this.IsDrawEdgeOn)
                OpenGLImports.glColorMask((byte)0, (byte)0, (byte)0, (byte)0);
            OpenGLImports.glEndList();
        }

        private void UpdateListRenderModeDepend()
        {
            if (!this.mIsLockGL || this.ActiveGLList > 0U)
                return;
            Color background = this.Palette.Background;
            Color forground = this.Palette.Forground;
            if (this.ListItems.texture2dlist == 0U)
                this.ListItems.texture2dlist = OpenGLImports.glGenLists(1);
            OpenGLImports.glNewList(this.ListItems.texture2dlist, OpenGLImports.ListMode.GL_COMPILE);
            if (this.RenderMode == vdRender.Mode.Render || this.RenderMode == vdRender.Mode.RenderOn)
                OpenGLImports.glEnable(OpenGLImports.GLCap.GL_TEXTURE_2D);
            else
                OpenGLImports.glDisable(OpenGLImports.GLCap.GL_TEXTURE_2D);
            OpenGLImports.glEndList();
        }

        private void UpdateListColors()
        {
            if (!this.mIsLockGL || this.ActiveGLList > 0U)
                return;
            Color background = this.Palette.Background;
            Color forground = this.Palette.Forground;
            if (this.ListItems.forcolorlist == 0U)
                this.ListItems.forcolorlist = OpenGLImports.glGenLists(1);
            OpenGLImports.glNewList(this.ListItems.forcolorlist, OpenGLImports.ListMode.GL_COMPILE);
            OpenGLImports.glColor4d((double)forground.R / (double)byte.MaxValue, (double)forground.G / (double)byte.MaxValue, (double)forground.B / (double)byte.MaxValue, 1.0);
            OpenGLImports.glEndList();
            if (this.ListItems.bkcolorlist == 0U)
                this.ListItems.bkcolorlist = OpenGLImports.glGenLists(1);
            OpenGLImports.glNewList(this.ListItems.bkcolorlist, OpenGLImports.ListMode.GL_COMPILE);
            OpenGLImports.glColor4d((double)background.R / (double)byte.MaxValue, (double)background.G / (double)byte.MaxValue, (double)background.B / (double)byte.MaxValue, 1.0);
            OpenGLImports.glEndList();
            if (this.ListItems.texturedisablelist == 0U)
                this.ListItems.texturedisablelist = OpenGLImports.glGenLists(1);
            OpenGLImports.glNewList(this.ListItems.texturedisablelist, OpenGLImports.ListMode.GL_COMPILE);
            OpenGLImports.glDisable(OpenGLImports.GLCap.GL_TEXTURE_2D);
            OpenGLImports.glEnable(OpenGLImports.GLCap.GL_COLOR_MATERIAL);
            OpenGLImports.glEndList();
            this.UpdateListRenderModeDepend();
            if (this.ListItems.ShadowTextureBind == 0U)
                this.ListItems.ShadowTextureBind = OpenGLImports.glGenLists(1);
            OpenGLImports.glNewList(this.ListItems.ShadowTextureBind, OpenGLImports.ListMode.GL_COMPILE);
            OpenGLImports.glEndList();
        }

        public override bool IsBoundaryClip(Box bound)
        {
            if (this.mShadowLightMode == vdRender.ShadowLightModeFlag.None)
                return base.IsBoundaryClip(bound);
            return bound != null && !bound.IsEmpty;
        }

        public override LightManager LManager => this.mShared != null ? this.mShared.LManager : base.LManager;

        private void ClearShadowResources()
        {
            for (int index = 0; index < this.LManager.ShaowLights.Count; ++index)
            {
                RenderingLightPropsEx renderingLightPropsEx = this.LManager[index];
                if (renderingLightPropsEx != null && OpenGLImports.glIsTexture(renderingLightPropsEx.shadowMapTexture) != (byte)0)
                {
                    OpenGLImports.glDeleteTextures(1, new uint[1]
                    {
            renderingLightPropsEx.shadowMapTexture
                    });
                    renderingLightPropsEx.shadowMapTexture = 0U;
                    renderingLightPropsEx.Update();
                }
            }
        }

        public override vdRender.ShadowLightModeFlag ShadowLightMode => this.mShadowLightMode;

        private bool SketchPureDrawArrays(vdglTypes.IBufferWrapper drawbuffer)
        {
            if (this.mEdgePass == vdrawglRender_opengl_2.EdgePassFlag.None)
                return false;
            if (this.GlobalProperties.EdgeEffect == vdRenderGlobalProperties.EdgeEffectFlag.DashedHidden)
                return !this.IsDrawEdgeOn;
            vdglTypes.IBufferWrapper bufferWrapper = drawbuffer;
            if (drawbuffer.type == 4 || drawbuffer.type == 7)
            {
                int num1 = 16;
                OpenGLImports.ComparisonFunction func = OpenGLImports.ComparisonFunction.GL_ALWAYS;
                int num2;
                OpenGLImports.Primitives mode;
                if (drawbuffer.type == 4)
                {
                    num2 = 3;
                    mode = OpenGLImports.Primitives.GL_TRIANGLES;
                }
                else
                {
                    num2 = 4;
                    mode = OpenGLImports.Primitives.GL_QUADS;
                }
                if (this.mEdgePass == vdrawglRender_opengl_2.EdgePassFlag.Pass2)
                {
                    func = OpenGLImports.ComparisonFunction.GL_NOTEQUAL;
                    if (this.GlobalProperties.EdgeEffect == vdRenderGlobalProperties.EdgeEffectFlag.AutoDetect)
                        mode = OpenGLImports.Primitives.GL_LINE_LOOP;
                }
                if (this.IsEdgeColorEmpty)
                {
                    OpenGLImports.glColor4d(bufferWrapper[4], bufferWrapper[5], bufferWrapper[6], 1.0);
                }
                else
                {
                    Color edgeColor = this.EdgeColor;
                    int r = (int)edgeColor.R;
                    edgeColor = this.EdgeColor;
                    int g = (int)edgeColor.G;
                    edgeColor = this.EdgeColor;
                    int b = (int)edgeColor.B;
                    this.SetColor(Color.FromArgb(r, g, b));
                }
                int num3 = num2 * num1;
                int num4 = drawbuffer.curitem / num1 / num2;
                double num5 = 42.0 * 0.5;
                for (int index1 = 0; index1 < num4; ++index1)
                {
                    int index2 = index1 * num3;
                    Vector v = new Vector(bufferWrapper[index2 + 8], bufferWrapper[index2 + 9], bufferWrapper[index2 + 10]);
                    this.View2Worldmatrix.TransformVector(v, true);
                    int refx = (int)(num5 * (1.0 + v.x) + 2.0 * num5 * (1.0 + v.y) + 3.0 * num5 * (1.0 + v.z));
                    OpenGLImports.glStencilFunc(func, refx, uint.MaxValue);
                    OpenGLImports.glBegin(mode);
                    if (this.GlobalProperties.EdgeEffect == vdRenderGlobalProperties.EdgeEffectFlag.OutLine)
                        OpenGLImports.glEdgeFlag((byte)1);
                    for (int index3 = 0; index3 < num2; ++index3)
                    {
                        OpenGLImports.glVertex3d(bufferWrapper[index2], bufferWrapper[index2 + 1], bufferWrapper[index2 + 2]);
                        index2 += num1;
                    }
                    OpenGLImports.glEnd();
                }
            }
            return true;
        }

        private vdRender.DrawStatus PureDrawSceneEdges(IDrawScene drawsceneObj)
        {
            vdRender.DrawStatus statusDraw = this.StatusDraw;
            if (statusDraw == vdRender.DrawStatus.Break || this.GlobalProperties.EdgeEffect == vdRenderGlobalProperties.EdgeEffectFlag.UserDefine || this.RenderMode != vdRender.Mode.Hide && this.RenderMode != vdRender.Mode.ShadeOn && this.RenderMode != vdRender.Mode.RenderOn)
                return statusDraw;
            if (this.GlobalProperties.EdgeEffect == vdRenderGlobalProperties.EdgeEffectFlag.DashedHidden)
            {
                vdRender.DrawStatus drawStatus = base.DrawScene(drawsceneObj);
                if (drawStatus != vdRender.DrawStatus.Break)
                {
                    this.FlushDrawBuffers(-1);
                    this.mEdgePass = vdrawglRender_opengl_2.EdgePassFlag.Pass1;
                    OpenGLImports.glPushAttrib(OpenGLImports.AttribMask.GL_LINE_BIT | OpenGLImports.AttribMask.GL_POLYGON_BIT | OpenGLImports.AttribMask.GL_DEPTH_BUFFER_BIT | OpenGLImports.AttribMask.GL_VIEWPORT_BIT);
                    OpenGLImports.glDepthRange(0.0, this.PerspectiveMod == vdRender.VdConstPerspectiveMod.PerspectON ? 1.01 : 0.99);
                    OpenGLImports.glDepthMask((byte)0);
                    OpenGLImports.glEnable(OpenGLImports.GLCap.GL_POLYGON_OFFSET_FILL);
                    OpenGLImports.glEnable(OpenGLImports.GLCap.GL_DEPTH_TEST);
                    OpenGLImports.glDepthFunc(OpenGLImports.ComparisonFunction.GL_GREATER);
                    OpenGLImports.glEnable(OpenGLImports.GLCap.GL_LINE_STIPPLE);
                    OpenGLImports.glLineStipple(1, (ushort)57344);
                    drawStatus = base.DrawScene(drawsceneObj);
                    OpenGLImports.glPopAttrib();
                    OpenGLImports.glLineStipple(1, OpenGLImports.LineStipple);
                    this.FlushDrawBuffers(-1);
                    this.mEdgePass = vdrawglRender_opengl_2.EdgePassFlag.None;
                }
                return drawStatus;
            }
            OpenGLImports.glFlush();
            this.mEdgePass = vdrawglRender_opengl_2.EdgePassFlag.Pass1;
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.TRANSPARENT_ORDER, new double[1]
            {
        (double) vdglTypes.PropertyValues.TEXTURE_OFF.GetHashCode()
            });
            vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.HAS_TRANSPARENT, new double[1]);
            vdRender.PolygonModeEnum polygonMode = this.PolygonMode;
            this.PolygonMode = vdRender.PolygonModeEnum.FILL;
            this.PushPenstyle(this.EdgeColor, true);
            bool bVal = this.EnableLighting(false);
            bool bvalue1 = this.EnableBufferId(true);
            bool bvalue2 = this.EnableColorBuffer(false);
            bool bvalue3 = this.EnableTexture(false);
            this.ClearDepthBuffer();
            OpenGLImports.glClearStencil(0);
            OpenGLImports.glClear(OpenGLImports.ClearMask.GL_STENCIL_BUFFER_BIT);
            if (this.GlobalProperties.EdgeEffect == vdRenderGlobalProperties.EdgeEffectFlag.OutLine)
                OpenGLImports.glPolygonOffset(1.1f, 0.5f);
            else
                OpenGLImports.glPolygonOffset(1.1f, 0.5f);
            OpenGLImports.glEnable(OpenGLImports.GLCap.GL_POLYGON_OFFSET_FILL);
            OpenGLImports.glStencilOp(OpenGLImports.StencilOp.GL_KEEP, OpenGLImports.StencilOp.GL_KEEP, OpenGLImports.StencilOp.GL_REPLACE);
            vdRender.DrawStatus drawStatus1 = base.DrawScene(drawsceneObj);
            this.FlushDrawBuffers(-1);
            OpenGLImports.glFlush();
            this.EnableColorBuffer(true);
            this.mEdgePass = vdrawglRender_opengl_2.EdgePassFlag.Pass2;
            if (drawStatus1 != vdRender.DrawStatus.Break)
            {
                this.SetLineWidth((double)this.DpiY * (double)this.GlobalProperties.EdgePenWidth);
                OpenGLImports.glStencilOp(OpenGLImports.StencilOp.GL_KEEP, OpenGLImports.StencilOp.GL_KEEP, OpenGLImports.StencilOp.GL_KEEP);
                if (!this.IsEdgeColorEmpty)
                {
                    int r = (int)this.EdgeColor.R;
                    Color edgeColor = this.EdgeColor;
                    int g = (int)edgeColor.G;
                    edgeColor = this.EdgeColor;
                    int b = (int)edgeColor.B;
                    this.SetColor(Color.FromArgb(r, g, b));
                }
                OpenGLImports.glDisable(OpenGLImports.GLCap.GL_POLYGON_OFFSET_FILL);
                if (this.GlobalProperties.EdgeEffect == vdRenderGlobalProperties.EdgeEffectFlag.OutLine)
                    OpenGLImports.glPolygonMode(OpenGLImports.FaceMode.GL_BACK, OpenGLImports.PolygonMode.GL_LINE);
                drawStatus1 = base.DrawScene(drawsceneObj);
                this.FlushDrawBuffers(-1);
            }
            this.PolygonMode = polygonMode;
            this.EnableColorBuffer(bvalue2);
            this.EnableBufferId(bvalue1);
            this.EnableLighting(bVal);
            this.EnableTexture(bvalue3);
            this.PopPenstyle();
            this.mEdgePass = vdrawglRender_opengl_2.EdgePassFlag.None;
            return drawStatus1;
        }

        private vdRender.DrawStatus PureDrawScene(IDrawScene drawsceneObj)
        {
            vdRender.DrawStatus statusDraw = this.StatusDraw;
            vdRender.DrawStatus drawStatus = base.DrawScene(drawsceneObj);
            this.FlushDrawBuffers(-1);
            return drawStatus;
        }

        public override vdRender.DrawStatus DrawScene(IDrawScene drawsceneObj)
        {
            vdRender.DrawStatus drawStatus1 = this.StatusDraw;
            if (!this.mIsLockGL || !this.ShadowSupported || this.LManager.ShaowLights.Count == 0)
            {
                drawStatus1 = this.PureDrawScene(drawsceneObj);
            }
            else
            {
                for (int index = 0; index < this.LManager.ShaowLights.Count; ++index)
                {
                    RenderingLightPropsEx renderingLightPropsEx = this.LManager[this.LManager.ShaowLights[index]];
                    if (renderingLightPropsEx.NeedupdateShadowTexture)
                    {
                        Size maxRenderingSize = this.OpenGLControlObject.MaxRenderingSize;
                        this.mShadowLightMode = vdRender.ShadowLightModeFlag.CreateShadow;
                        Matrix matrix1 = new Matrix();
                        matrix1.SetLookAt(renderingLightPropsEx.light.Position, renderingLightPropsEx.light.Position + (gPoint)renderingLightPropsEx.light.Direction, 0.0);
                        vdRender.VdConstPerspectiveMod perspectivemod = vdRender.VdConstPerspectiveMod.PerspectOFF;
                        if (renderingLightPropsEx.light.TypeOfLight == LightType.Spot)
                            perspectivemod = vdRender.VdConstPerspectiveMod.PerspectON;
                        gPoint viewcenter;
                        double viewSize;
                        vdRender._GetViewExents(this.DrawingExtents, maxRenderingSize.Width, maxRenderingSize.Height, perspectivemod, matrix1, renderingLightPropsEx.light.SpotAngle, 0.05, out viewcenter, out viewSize);
                        matrix1.TranslateMatrix(viewcenter * -1.0);
                        viewcenter = new gPoint();
                        Matrix projectionMatrix = vdRender._GetProjectionMatrix(perspectivemod, maxRenderingSize.Width, maxRenderingSize.Height, viewSize, viewcenter, this.DrawingExtents, matrix1, (gPoint)null, renderingLightPropsEx.light.SpotAngle, 0.05, out Vector _);
                        projectionMatrix.ScaleMatrix(1.0, -1.0, 1.0);
                        Matrix matrix2 = OpenGLImports.OpenGL2VdrawMatrix(new double[16]
                        {
              0.5,
              0.0,
              0.0,
              0.0,
              0.0,
              0.5,
              0.0,
              0.0,
              0.0,
              0.0,
              0.5,
              0.0,
              0.5,
              0.5,
              0.5,
              1.0
                        });
                        renderingLightPropsEx.shadowMatrix = matrix1 * projectionMatrix * matrix2;
                        this.PushMatrix(matrix1 * this.View2Worldmatrix);
                        this.UpdateProperties();
                        this.ClearTextureInternalLists();
                        this.OnImageBind(IntPtr.Zero, vdglTypes.MATERIAL_FLAG.NONE);
                        this.EnableColorBuffer(false);
                        this.EnableLighting(false);
                        this.EnableTexture(false);
                        this.ClearDepthBuffer();
                        vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.POLYGONMODE, new double[1]
                        {
              (double) vdRender.PolygonModeEnum.FILL.GetHashCode()
                        });
                        vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.TRANSPARENT_ORDER, new double[1]
                        {
              (double) vdglTypes.PropertyValues.POLYGON_MODE_LINES.GetHashCode()
                        });
                        OpenGLImports.glDisable(OpenGLImports.GLCap.GL_ALPHA_TEST);
                        OpenGLImports.glAlphaFunc(OpenGLImports.ComparisonFunction.GL_GEQUAL, 0.9999f);
                        OpenGLImports.glMatrixMode(OpenGLImports.MatrixMode.GL_PROJECTION);
                        OpenGLImports.glLoadMatrixd(OpenGLImports.GetGLMatrix(projectionMatrix));
                        OpenGLImports.glViewport(0, 0, maxRenderingSize.Width, maxRenderingSize.Height);
                        OpenGLImports.glMatrixMode(OpenGLImports.MatrixMode.GL_MODELVIEW);
                        OpenGLImports.glPolygonOffset(4f, 4f);
                        OpenGLImports.glEnable(OpenGLImports.GLCap.GL_POLYGON_OFFSET_FILL);
                        OpenGLImports.glEnable(OpenGLImports.GLCap.GL_COLOR_MATERIAL);
                        MessageManager.BreakMessageMethod breakOnMessage = this.BreakOnMessage;
                        this.BreakOnMessage = MessageManager.BreakMessageMethod.None;
                        drawStatus1 = base.DrawScene(drawsceneObj);
                        this.BreakOnMessage = breakOnMessage;
                        this.FlushDrawBuffers(-1);
                        this.mShadowLightMode = vdRender.ShadowLightModeFlag.None;
                        if (OpenGLImports.glIsTexture(renderingLightPropsEx.shadowMapTexture) == (byte)0)
                        {
                            uint[] textures = new uint[1];
                            OpenGLImports.glGenTextures(1, textures);
                            renderingLightPropsEx.shadowMapTexture = textures[0];
                            OpenGLImports.glBindTexture(OpenGLImports.TargetTexture.GL_TEXTURE_2D, renderingLightPropsEx.shadowMapTexture);
                            OpenGLImports.glTexImage2D(OpenGLImports.TargetTexture.GL_TEXTURE_2D, 0, OpenGLImports.PixelFormats.GL_DEPTH_COMPONENT, maxRenderingSize.Width, maxRenderingSize.Height, 0, OpenGLImports.TexturePixelFormat.GL_DEPTH_COMPONENT, OpenGLImports.PixelType.GL_FLOAT, IntPtr.Zero);
                        }
                        else
                            OpenGLImports.glBindTexture(OpenGLImports.TargetTexture.GL_TEXTURE_2D, renderingLightPropsEx.shadowMapTexture);
                        OpenGLImports.glFlush();
                        OpenGLImports.glCopyTexSubImage2D(OpenGLImports.TargetTexture.GL_TEXTURE_2D, 0, 0, 0, 0, 0, maxRenderingSize.Width, maxRenderingSize.Height);
                        this.ClearDepthBuffer();
                        this.UpdateListColors();
                        this.PopMatrix();
                        this.UpdateProperties();
                        this.PrepareRenderMode();
                    }
                }
                this.mShadowLightMode = vdRender.ShadowLightModeFlag.RenderScene;
                for (int cap = 16384; cap <= 16391; ++cap)
                    OpenGLImports.glDisable((OpenGLImports.GLCap)cap);
                for (int index = 0; index < this.LManager.Count; ++index)
                {
                    if (this.LManager[index].light.ApplyShadow)
                        this.DrawLight((object)this, this.LManager[index].light);
                }
                vdRender.DrawStatus drawStatus2 = this.PureDrawScene(drawsceneObj);
                OpenGLImports.glFlush();
                this.mShadowLightMode = vdRender.ShadowLightModeFlag.None;
                this.PrepareRenderMode();
                for (int index = 0; index < this.LManager.Count; ++index)
                    this.DrawLight((object)this, this.LManager[index].light);
                for (int index = 0; index < this.LManager.ShaowLights.Count; ++index)
                {
                    RenderingLightPropsEx renderingLightPropsEx = this.LManager[this.LManager.ShaowLights[index]];
                    if (drawStatus2 != vdRender.DrawStatus.Break)
                    {
                        this.mShadowLightMode = vdRender.ShadowLightModeFlag.ApplyShadow;
                        double num1 = 0.0;
                        double num2 = 1.0;
                        Matrix shadowMatrix = renderingLightPropsEx.shadowMatrix;
                        OpenGLImports.glTexGeni(OpenGLImports.TextureCoordName.GL_S, OpenGLImports.TextureGenParameter.GL_TEXTURE_GEN_MODE, OpenGLImports.TextureGenMode.GL_EYE_LINEAR);
                        OpenGLImports.glTexGendv(OpenGLImports.TextureCoordName.GL_S, OpenGLImports.TextureGenParameter.GL_EYE_PLANE, new double[4]
                        {
              shadowMatrix.A00,
              shadowMatrix.A01,
              shadowMatrix.A02,
              shadowMatrix.A03
                        });
                        OpenGLImports.glEnable(OpenGLImports.GLCap.GL_TEXTURE_GEN_S);
                        OpenGLImports.glTexGeni(OpenGLImports.TextureCoordName.GL_T, OpenGLImports.TextureGenParameter.GL_TEXTURE_GEN_MODE, OpenGLImports.TextureGenMode.GL_EYE_LINEAR);
                        OpenGLImports.glTexGendv(OpenGLImports.TextureCoordName.GL_T, OpenGLImports.TextureGenParameter.GL_EYE_PLANE, new double[4]
                        {
              shadowMatrix.A10,
              shadowMatrix.A11,
              shadowMatrix.A12,
              shadowMatrix.A13
                        });
                        OpenGLImports.glEnable(OpenGLImports.GLCap.GL_TEXTURE_GEN_T);
                        OpenGLImports.glTexGeni(OpenGLImports.TextureCoordName.GL_R, OpenGLImports.TextureGenParameter.GL_TEXTURE_GEN_MODE, OpenGLImports.TextureGenMode.GL_EYE_LINEAR);
                        OpenGLImports.glTexGendv(OpenGLImports.TextureCoordName.GL_R, OpenGLImports.TextureGenParameter.GL_EYE_PLANE, new double[4]
                        {
              shadowMatrix.A20,
              shadowMatrix.A21,
              shadowMatrix.A22 * num2,
              shadowMatrix.A23 + num1
                        });
                        OpenGLImports.glEnable(OpenGLImports.GLCap.GL_TEXTURE_GEN_R);
                        OpenGLImports.glTexGeni(OpenGLImports.TextureCoordName.GL_Q, OpenGLImports.TextureGenParameter.GL_TEXTURE_GEN_MODE, OpenGLImports.TextureGenMode.GL_EYE_LINEAR);
                        OpenGLImports.glTexGendv(OpenGLImports.TextureCoordName.GL_Q, OpenGLImports.TextureGenParameter.GL_EYE_PLANE, new double[4]
                        {
              shadowMatrix.A30,
              shadowMatrix.A31,
              shadowMatrix.A32,
              shadowMatrix.A33
                        });
                        OpenGLImports.glEnable(OpenGLImports.GLCap.GL_TEXTURE_GEN_Q);
                        OpenGLImports.glBindTexture(OpenGLImports.TargetTexture.GL_TEXTURE_2D, renderingLightPropsEx.shadowMapTexture);
                        OpenGLImports.glEnable(OpenGLImports.GLCap.GL_TEXTURE_2D);
                        OpenGLImports.glTexParameteri(OpenGLImports.TargetTexture.GL_TEXTURE_2D, OpenGLImports.TextureParameterName.GL_TEXTURE_MIN_FILTER, OpenGLImports.TextureFilters.GL_LINEAR);
                        OpenGLImports.glTexParameteri(OpenGLImports.TargetTexture.GL_TEXTURE_2D, OpenGLImports.TextureParameterName.GL_TEXTURE_MAG_FILTER, OpenGLImports.TextureFilters.GL_LINEAR);
                        OpenGLImports.glTexParameteri(OpenGLImports.TargetTexture.GL_TEXTURE_2D, OpenGLImports.TextureParameterName.GL_TEXTURE_WRAP_S, OpenGLImports.TextureFilters.GL_CLAMP);
                        OpenGLImports.glTexParameteri(OpenGLImports.TargetTexture.GL_TEXTURE_2D, OpenGLImports.TextureParameterName.GL_TEXTURE_WRAP_T, OpenGLImports.TextureFilters.GL_CLAMP);
                        OpenGLImports.glTexParameteri(OpenGLImports.TargetTexture.GL_TEXTURE_2D, OpenGLImports.TextureParameterName.GL_TEXTURE_COMPARE_MODE_ARB, OpenGLImports.TextureFilters.GL_COMPARE_R_TO_TEXTURE);
                        OpenGLImports.glTexParameteri(OpenGLImports.TargetTexture.GL_TEXTURE_2D, OpenGLImports.TextureParameterName.GL_TEXTURE_COMPARE_FUNC_ARB, OpenGLImports.TextureFilters.GL_LEQUAL);
                        OpenGLImports.glTexParameteri(OpenGLImports.TargetTexture.GL_TEXTURE_2D, OpenGLImports.TextureParameterName.GL_DEPTH_TEXTURE_MODE_ARB, OpenGLImports.TextureFilters.GL_INTENSITY);
                        OpenGLImports.glAlphaFunc(OpenGLImports.ComparisonFunction.GL_GEQUAL, 0.9999f);
                        OpenGLImports.glEnable(OpenGLImports.GLCap.GL_ALPHA_TEST);
                        this.ClearTextureInternalLists();
                        if (this.RenderMode == vdRender.Mode.Render || this.RenderMode == vdRender.Mode.RenderOn)
                        {
                            OpenGLImports.glDisable(OpenGLImports.GLCap.GL_COLOR_MATERIAL);
                            OpenGLImports.glMaterialfv(OpenGLImports.FaceMode.GL_FRONT_AND_BACK, OpenGLImports.MaterialParameter.GL_SPECULAR, new float[4]
                            {
                1f,
                1f,
                1f,
                1f
                            });
                            OpenGLImports.glMaterialfv(OpenGLImports.FaceMode.GL_FRONT_AND_BACK, OpenGLImports.MaterialParameter.GL_AMBIENT_AND_DIFFUSE, new float[4]
                            {
                1f,
                1f,
                1f,
                1f
                            });
                            OpenGLImports.glEnable(OpenGLImports.GLCap.GL_BLEND);
                            OpenGLImports.glBlendFunc(OpenGLImports.BlendingFactorSrc.GL_DST_COLOR, OpenGLImports.BlendingFactorDest.GL_ONE);
                        }
                        else
                        {
                            OpenGLImports.glEnable(OpenGLImports.GLCap.GL_BLEND);
                            OpenGLImports.glBlendFunc(OpenGLImports.BlendingFactorSrc.GL_ONE, OpenGLImports.BlendingFactorDest.GL_ZERO);
                        }
                        vdgl.SetPropertyValue(this.vdContext, vdglTypes.PropertyType.TRANSPARENT_ORDER, new double[1]
                        {
              (double) vdglTypes.PropertyValues.TEXTURE_OFF.GetHashCode()
                        });
                        if (this.ListItems.ShadowTextureBind == 0U)
                            this.ListItems.ShadowTextureBind = OpenGLImports.glGenLists(1);
                        OpenGLImports.glNewList(this.ListItems.ShadowTextureBind, OpenGLImports.ListMode.GL_COMPILE);
                        OpenGLImports.glEnable(OpenGLImports.GLCap.GL_TEXTURE_2D);
                        OpenGLImports.glBindTexture(OpenGLImports.TargetTexture.GL_TEXTURE_2D, renderingLightPropsEx.shadowMapTexture);
                        OpenGLImports.glEndList();
                        drawStatus2 = base.DrawScene(drawsceneObj);
                        this.FlushDrawBuffers(-1);
                        OpenGLImports.glFlush();
                        this.mShadowLightMode = vdRender.ShadowLightModeFlag.None;
                        OpenGLImports.glDisable(OpenGLImports.GLCap.GL_TEXTURE_GEN_S);
                        OpenGLImports.glDisable(OpenGLImports.GLCap.GL_TEXTURE_GEN_T);
                        OpenGLImports.glDisable(OpenGLImports.GLCap.GL_TEXTURE_GEN_R);
                        OpenGLImports.glDisable(OpenGLImports.GLCap.GL_TEXTURE_GEN_Q);
                        OpenGLImports.glMaterialfv(OpenGLImports.FaceMode.GL_FRONT_AND_BACK, OpenGLImports.MaterialParameter.GL_DIFFUSE, new float[4]
                        {
              0.2f,
              0.2f,
              0.2f,
              1f
                        });
                        OpenGLImports.glMaterialfv(OpenGLImports.FaceMode.GL_FRONT_AND_BACK, OpenGLImports.MaterialParameter.GL_AMBIENT, new float[4]
                        {
              0.8f,
              0.8f,
              0.8f,
              1f
                        });
                        OpenGLImports.glMaterialfv(OpenGLImports.FaceMode.GL_FRONT_AND_BACK, OpenGLImports.MaterialParameter.GL_SPECULAR, new float[4]
                        {
              0.0f,
              0.0f,
              0.0f,
              1f
                        });
                        this.UpdateListColors();
                        this.PrepareRenderMode();
                    }
                    else
                        break;
                }
                for (int index = 0; index < this.LManager.Count; ++index)
                    this.DrawLight((object)this, this.LManager[index].light);
            }
            vdRender.DrawStatus drawStatus3 = this.PureDrawSceneEdges(drawsceneObj);
            this.StatusDraw = drawStatus3;
            this.PrepareRenderMode();
            this.FinishDrawScene();
            return drawStatus3;
        }

        private enum EdgePassFlag
        {
            None,
            Pass1,
            Pass2,
        }
    }
}
