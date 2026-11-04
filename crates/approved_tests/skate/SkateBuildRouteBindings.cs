// Controlled engine/transport boundaries for the actual SkatePieces and SkateHeld source.
// No Unity renderer, native Planner, server placement, payment or inventory movement executes.
using System; using System.Collections.Generic;
namespace UnityEngine {
 public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} public static Vector2 zero=>default; public static Vector2 operator +(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y); public static Vector2 operator *(Vector2 a,float f)=>new Vector2(a.x*f,a.y*f);public static Vector2 ClampMagnitude(Vector2 a,float f)=>a; }
 public struct Vector3 {public float x,y,z;public static Vector3 zero=>default;public static float Distance(Vector3 a,Vector3 b)=>0;}
 public struct Rect {public bool Contains(Vector2 p)=>true;}
 public class Transform {public Vector3 position,forward,eulerAngles;}
 public class Camera {public static Camera main;public Transform transform=new Transform();}
 public struct RaycastHit {public Vector3 normal,point;}
 public enum QueryTriggerInteraction {Ignore} public enum LogType {Log}
 public static class Physics {public static bool Raycast(Vector3 a,Vector3 b,out RaycastHit h,float reach,int mask,QueryTriggerInteraction q){h=default;return false;}}
 public static class Screen {public static int height=1000;}
 public static class Time {public static float realtimeSinceStartup=100;}
 public static class Application {
  public static bool isFocused=true;
  public sealed class LogCallback {
   internal Action<string,string,LogType> Callback;
   public static implicit operator LogCallback(Action<string,string,LogType> a){return new LogCallback {Callback=a};}
  }
  private static readonly List<LogCallback> callbacks=new List<LogCallback>();
  public static void add_logMessageReceived(LogCallback c){callbacks.Add(c);}
  public static void remove_logMessageReceived(LogCallback c){callbacks.Remove(c);}
  public static void Emit(string text){foreach(var c in callbacks.ToArray())c.Callback(text,"",LogType.Log);}
 }
}
namespace UnityEngine.InputSystem {
 public class Button {public bool isPressed,wasPressedThisFrame,wasReleasedThisFrame;}
 public class Vec {public UnityEngine.Vector2 Value;public UnityEngine.Vector2 ReadValue()=>Value;}
 public class Keyboard {public Button pKey=new Button(),leftShiftKey=new Button(),rightShiftKey=new Button(),escapeKey=new Button(),f1Key=new Button(),tabKey=new Button(),backspaceKey=new Button(),rightArrowKey=new Button(),leftArrowKey=new Button(),enterKey=new Button(),oKey=new Button(),rKey=new Button();}
 public class Mouse {public static Mouse current;public Button leftButton=new Button(),rightButton=new Button();public Vec position=new Vec(),delta=new Vec();}
}

public class BaseCombatEntity {public enum LifeState {Alive,Dead}public LifeState lifestate=LifeState.Alive;}
public class BasePlayer:BaseCombatEntity {
 public Item Current; public Inventory inventory=new Inventory(); public bool ActiveReadFails;
 public Item GetActiveItem(){if(ActiveReadFails)throw new NotSupportedException("Controlled unavailable native item reader");return Current;}
}
public class Item {public ItemInfo info=new ItemInfo();public ItemData instanceData=new ItemData(); public string name;public ulong skin;public int amount=1,position;}
public class ItemInfo {public string shortname="building.planner";}
public class ItemData {public int dataInt;}
public class Inventory {public Container containerBelt=new Container(),containerMain=new Container();}
public class Container {public List<Item> itemList=new List<Item>();}
// Logical observations use the ordinary XInput bit values; no device reader is run.
public static class SkatePad {public const int Back=0x20,X=0x4000,B=0x2000,A=0x1000,Y=0x8000,DpadRight=0x8,DpadDown=0x2,DpadLeft=0x4,DpadUp=0x1,RightBumper=0x200,LeftBumper=0x100;public const float Pulled=.3f;public static bool Shared=true;public static int Buttons;public static float RightTrigger,RightX,RightY;}
public static class SkateRig {public static bool Ready=true;public static BasePlayer Local=new BasePlayer();public static object Body=new object();public static UnityEngine.Transform LocalT=new UnityEngine.Transform();public static float AwakeAt=1;}
public static class SkateRide {public static bool On;public static int GroundMask;public static void StanceHold(ref float a,bool b,float t){}}
public static class Out {public static void Say(string s){}}
public static partial class SkateHeld {
 public static bool Planner=true,AllowClose=true,Waiting,Closed=true,AllowCursor=true;
 public static int Acquires,Resumes; public static object Plan=new object(); public static string RustWheelGate="controlled boundary";
 public static bool HasPlanner(object p){return p!=null&&Planner;}
 public static bool RustWheelOpen(object p){return !Closed;}
 public static bool RustWheelHiddenForSkate(object p){return Closed;}
 public static bool CloseRustWheel(object p,bool expected,out bool waiting,out string why){waiting=Waiting;why="controlled boundary";if(AllowClose)Closed=true;return AllowClose;}
 public static bool RustWheelClosed(object p,out string why){why="controlled boundary";return Closed;}
 public static bool PausePlanner(object p,out string why){why="controlled boundary";return Planner;}
 public static object PausedPlanner(object p){return Plan;}
 public static bool AcquireSkateCursor(object p,out string why){why="controlled boundary";Acquires++;return AllowCursor&&Closed;}
 public static void ReleaseSkateCursor(bool all=false){} public static void ResumePlanner(){Resumes++;}
 public static float PlannerTurn(object p){return 0;}
}
public static class SkateConsole {public static bool CanSend=true,SendOk=true;public static string SendState="probe";public static List<string> Commands=new List<string>();public static bool Send(string s){Commands.Add(s);return SendOk;}}
public static class SkateBuildWheel {public static bool Available=true;public const float PointerRadius=100;public static string[] Labels=new string[12];public static UnityEngine.Vector2 PointerFor(int h)=>new UnityEngine.Vector2(0,100);}
public static class SkateParks {public enum Kind {HalfWallKicker=10} public static void PreviewClear(){} public static void PreviewAt(Kind k,UnityEngine.Vector3 p,float y,bool b){}public static void PreviewMove(UnityEngine.Vector3 p,float y){} public static bool HalfWallAim(UnityEngine.Vector3 a,UnityEngine.Vector3 b,UnityEngine.Vector3 c,out UnityEngine.Vector3 o,out float y,out string why){o=default;y=0;why="probe";return false;}public static bool Ghost(Kind k,UnityEngine.RaycastHit h,float y0,float turn,out UnityEngine.Vector3 o,out float y,out bool host,out string why,out Shortcut.RustMod.SkateParkTransform t,bool builder){o=default;y=0;host=false;why="probe";t=default;return false;}}
public static class SkateSupportFootprint {public static void Hide(){}public static void Reset(){}public static void Show(Shortcut.RustMod.SkateParkKind k,Shortcut.RustMod.SkateParkTransform t,bool v){}}

// Analog-sector math and GUI drawing are outside this D-pad/keyboard route scenario.
public static class SkateWheelChoice {public static int Sector(double x,double y,double deadzone){throw new InvalidOperationException("Analog radial is outside this route fixture");}}
