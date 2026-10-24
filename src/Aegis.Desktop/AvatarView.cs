using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
namespace Aegis.Desktop;
public sealed class AvatarView : UserControl
{
    public static readonly DependencyProperty ActiveProperty=DependencyProperty.Register(nameof(Active),typeof(bool),typeof(AvatarView),new PropertyMetadata(false,(d,_)=>((AvatarView)d).UpdateTimer()));
    public bool Active{get=>(bool)GetValue(ActiveProperty);set=>SetValue(ActiveProperty,value);}
    private readonly DispatcherTimer timer=new(){Interval=TimeSpan.FromMilliseconds(50)};
    private readonly AxisAngleRotation3D rotation=new(new Vector3D(0,1,0),0);
    public AvatarView()
    {
        var viewport=new Viewport3D {Camera=new PerspectiveCamera(new(0,.8,12),new(0,-.8,-12),new(0,1,0),48)};
        var root=new Model3DGroup();root.Children.Add(new AmbientLight(Color.FromRgb(70,90,120)));root.Children.Add(new DirectionalLight(Colors.White,new(-1,-1,-2)));root.Children.Add(new PointLight(Color.FromRgb(82,218,238),new(0,2,3)));
        var model=new Model3DGroup{Transform=new RotateTransform3D(rotation)};
        var core=new MeshGeometry3D {Positions=new Point3DCollection{new(0,1.2,0),new(0,-1.2,0),new(.65,0,0),new(0,0,.65),new(-.65,0,0),new(0,0,-.65)},TriangleIndices=new Int32Collection{0,2,3,0,3,4,0,4,5,0,5,2,1,3,2,1,4,3,1,5,4,1,2,5}};
        model.Children.Add(Make(core,Color.FromRgb(64,190,222),true));
        model.Children.Add(Arc(.45,.5,0,Color.FromRgb(82,218,238)));model.Children.Add(Arc(1.3,-.45,2,Color.FromRgb(160,186,216)));model.Children.Add(Arc(2,.15,4,Color.FromRgb(166,135,240)));
        root.Children.Add(model);viewport.Children.Add(new ModelVisual3D{Content=root});Content=viewport;
        timer.Tick+=(_,_)=>rotation.Angle=(rotation.Angle+.4)%360;
        Loaded+=(_,_)=>UpdateTimer();Unloaded+=(_,_)=>timer.Stop();IsVisibleChanged+=(_,_)=>UpdateTimer();
    }
    private void UpdateTimer(){if(Active&&IsLoaded&&IsVisible)timer.Start();else timer.Stop();}
    private static GeometryModel3D Make(MeshGeometry3D mesh,Color color,bool glow=false)
    {
        var materials=new MaterialGroup();materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));materials.Children.Add(new SpecularMaterial(new SolidColorBrush(Colors.White),80));if(glow)materials.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromArgb(70,color.R,color.G,color.B))));
        return new(mesh,materials){BackMaterial=materials};
    }
    private static GeometryModel3D Arc(double tilt,double rotation,double phase,Color color)
    {
        var mesh=new MeshGeometry3D();const int segments=56,sides=8;
        for(int i=0;i<=segments;i++)for(int j=0;j<sides;j++)
        {
            double a=phase+i/(double)segments*Math.PI*1.65,b=j/(double)sides*Math.PI*2;
            double x=(1.7+.065*Math.Cos(b))*Math.Cos(a),y=.065*Math.Sin(b),z=(1.7+.065*Math.Cos(b))*Math.Sin(a);
            (y,z)=(y*Math.Cos(tilt)-z*Math.Sin(tilt),y*Math.Sin(tilt)+z*Math.Cos(tilt));(x,y)=(x*Math.Cos(rotation)-y*Math.Sin(rotation),x*Math.Sin(rotation)+y*Math.Cos(rotation));mesh.Positions.Add(new(x,y,z));
        }
        for(int i=0;i<segments;i++)for(int j=0;j<sides;j++){int a=i*sides+j,b=i*sides+(j+1)%sides,c=b+sides,d=a+sides;foreach(int v in new[]{a,b,c,a,c,d})mesh.TriangleIndices.Add(v);}
        return Make(mesh,color);
    }
}
