﻿#pragma checksum "..\..\CADView.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "2221756F63D8816CB4741FB29FFCC094"
//------------------------------------------------------------------------------
// <auto-generated>
//     此代码由工具生成。
//     运行时版本:4.0.30319.42000
//
//     对此文件的更改可能会导致不正确的行为，并且如果
//     重新生成代码，这些更改将会丢失。
// </auto-generated>
//------------------------------------------------------------------------------

using SharpGL.WPF;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace CADCtrl {
    
    
    /// <summary>
    /// CADView
    /// </summary>
    public partial class CADView : System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector {
        
        
        #line 10 "..\..\CADView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal SharpGL.WPF.OpenGLControl openGLCtrl;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/CADCtrl;component/cadview.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\CADView.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.openGLCtrl = ((SharpGL.WPF.OpenGLControl)(target));
            
            #line 11 "..\..\CADView.xaml"
            this.openGLCtrl.OpenGLDraw += new SharpGL.SceneGraph.OpenGLEventHandler(this.OpenGLControl_OpenGLDraw);
            
            #line default
            #line hidden
            
            #line 12 "..\..\CADView.xaml"
            this.openGLCtrl.OpenGLInitialized += new SharpGL.SceneGraph.OpenGLEventHandler(this.OpenGLControl_OpenGLInitialized);
            
            #line default
            #line hidden
            
            #line 13 "..\..\CADView.xaml"
            this.openGLCtrl.MouseDown += new System.Windows.Input.MouseButtonEventHandler(this.OpenGLControl_MouseDown);
            
            #line default
            #line hidden
            
            #line 14 "..\..\CADView.xaml"
            this.openGLCtrl.MouseUp += new System.Windows.Input.MouseButtonEventHandler(this.OpenGLControl_MouseUp);
            
            #line default
            #line hidden
            
            #line 15 "..\..\CADView.xaml"
            this.openGLCtrl.MouseMove += new System.Windows.Input.MouseEventHandler(this.OpenGLControl_MouseMove);
            
            #line default
            #line hidden
            
            #line 16 "..\..\CADView.xaml"
            this.openGLCtrl.MouseWheel += new System.Windows.Input.MouseWheelEventHandler(this.OpenGLControl_MouseWheel);
            
            #line default
            #line hidden
            
            #line 18 "..\..\CADView.xaml"
            this.openGLCtrl.Loaded += new System.Windows.RoutedEventHandler(this.UserControl_Loaded);
            
            #line default
            #line hidden
            
            #line 19 "..\..\CADView.xaml"
            this.openGLCtrl.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(this.UserControl_PreviewKeyDown);
            
            #line default
            #line hidden
            
            #line 20 "..\..\CADView.xaml"
            this.openGLCtrl.PreviewKeyUp += new System.Windows.Input.KeyEventHandler(this.UserControl_PreviewKeyUp);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

