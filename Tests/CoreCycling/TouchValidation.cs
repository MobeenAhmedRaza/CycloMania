using System;
using System.Reflection;
using Cyclomania;
using SBPScripts;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class TouchValidation
{
    static void Invoke(object target,string method) => target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
    static int count;
    static void Check(bool pass,string label) { if(!pass) throw new Exception(label); count++; Debug.Log("PASS: "+label); }
    public static void Run(BicycleController bike)
    {
        var events=new GameObject("EventSystem").AddComponent<EventSystem>();
        MobileButtonHandler Control(string name,int direction)
        {
            var g=new GameObject(name,typeof(RectTransform));
            var button=g.AddComponent<MobileButtonHandler>();button.direction=direction;return button;
        }
        PointerEventData Finger(int id) => new PointerEventData(events) {pointerId=id,button=PointerEventData.InputButton.Left};
        var pedal=Control("Pedal test",1);var steer=Control("Steer test",-1);var brake=Control("Brake test",-1);
        pedal.OnPointerDown(Finger(1));steer.OnPointerDown(Finger(2));
        Check(pedal.buttonPressed==1 && steer.buttonPressed==-1,"two controls hold independently");
        steer.OnPointerUp(Finger(2));Check(pedal.IsPressed && !steer.IsPressed,"releasing steering preserves pedaling");
        pedal.OnPointerDown(Finger(3));pedal.OnPointerUp(Finger(1));
        Check(pedal.IsPressed,"second finger on same control preserves hold");
        pedal.OnPointerUp(Finger(99));Check(pedal.IsPressed,"unrelated pointer cannot release hold");
        pedal.OnPointerExit(Finger(3));Check(!pedal.IsPressed && pedal.buttonPressed==0,"sliding off releases hold");
        bike.mobileControls.forward=pedal;bike.mobileControls.backward=brake;bike.mobileControls.left=steer;
        pedal.OnPointerDown(Finger(1));brake.OnPointerDown(Finger(2));Invoke(bike,"ApplyCustomInput");
        Check(bike.rawCustomAccelerationAxis==-1,"brake takes priority over held pedal");
        bike.ClearInput();Check(!pedal.IsPressed && !brake.IsPressed,"controller reset clears pointer ownership");
        pedal.OnPointerDown(Finger(1));pedal.enabled=false;Check(!pedal.IsPressed,"disabled controls release holds");pedal.enabled=true;
        pedal.OnPointerDown(Finger(1));Time.timeScale=0;Invoke(pedal,"Update");pedal.OnPointerDown(Finger(2));
        Check(!pedal.IsPressed,"pause clears and rejects held input");Time.timeScale=1;
        pedal.OnPointerDown(Finger(1));
        typeof(MobileButtonHandler).GetMethod("OnApplicationFocus",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(pedal,new object[]{false});
        Check(!pedal.IsPressed,"focus loss releases input");
        pedal.OnPointerDown(Finger(1));typeof(MobileButtonHandler).GetField("screenWidth",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(pedal,-1);Invoke(pedal,"Update");
        Check(!pedal.IsPressed,"screen change releases old touches");
        var jump=Control("Jump test",1);bike.mobileControls.bHop=jump;
        jump.OnPointerDown(Finger(5));Invoke(bike,"ApplyCustomInput");jump.ReleaseAll();Invoke(bike,"ApplyCustomInput");
        Check(bike.bunnyHopInputState==0,"cancelled jump hold does not launch a jump");
        jump.OnPointerDown(Finger(5));Invoke(bike,"ApplyCustomInput");jump.OnPointerUp(Finger(5));Invoke(bike,"ApplyCustomInput");
        Check(bike.bunnyHopInputState==-1,"intentional jump release preserves jump action");
        int presses=0;pedal.onPress.AddListener(()=>presses++);pedal.OnPointerDown(Finger(1));pedal.OnPointerDown(Finger(2));
        Check(presses==1,"one-shot action fires once per hold");pedal.ReleaseAll();
        foreach(TouchControlGraphic.Symbol symbol in Enum.GetValues(typeof(TouchControlGraphic.Symbol)))
        {
            var control=Control(symbol.ToString(),1);var graphic=control.gameObject.AddComponent<TouchControlGraphic>();graphic.symbol=symbol;
            ((RectTransform)control.transform).sizeDelta=Vector2.one*180;
            using(var helper=new VertexHelper())
            {
                typeof(TouchControlGraphic).GetMethod("OnPopulateMesh",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(VertexHelper)},null).Invoke(graphic,new object[]{helper});
                Check(helper.currentVertCount>400,"valid control mesh: "+symbol);
            }
        }
        Debug.Log("TOUCH_VALIDATION_SUCCESS "+count);
    }
}
