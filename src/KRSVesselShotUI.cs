using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KronalUtils
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class KRSVesselShotUI : MonoBehaviour
    {
        private KRSVesselShot control = new KRSVesselShot();
        bool mySoftLock = false;//not to be confused with EditorLogic.softLock
        private string inputLockIdent = "KVV-EditorLock";
        private Rect windowSize;
        private Vector2 windowScrollPos;
        private int tabCurrent;//almost obsolete
        private int shaderTabCurrent;
        private string[] shaderTabsNames;
        private Rect orthoViewRect;
        private GUIStyle guiStyleButtonAlert;
        private ApplicationLauncherButton KVVButton;
        private bool visible;
        private KRSEditorAxis axis;
        private bool IsOnEditor()
        {
            //return (HighLogic.LoadedScene == GameScenes.EDITOR || HighLogic.LoadedScene == GameScenes.SPH);
            return (HighLogic.LoadedScene == GameScenes.EDITOR || HighLogic.LoadedSceneIsEditor);
        }

        public void Awake()
        {
            this.windowSize = new Rect(256f, 50f, 300f, Screen.height - 50f);
            string[] configAppend = {"Part Config"};
            this.shaderTabsNames = this.control.Effects.Keys.ToArray<string>();
            this.shaderTabsNames = this.shaderTabsNames.Concat(configAppend).ToArray();
            this.control.Config.onApply += ConfigApplied;
            this.control.Config.onRevert += ConfigReverted;

            GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
        }

        private void Start()
        {
            if (KVVButton == null)
            {
                this.OnGUIAppLauncherReady();
            }
        }

        private void ConfigApplied()
        {
            ButtonMode(false);
        }

        private void ConfigReverted()
        {
            EditorLogic.fetch.Unlock(GetInstanceID().ToString());
            ButtonMode(true);
        }
        
        private void ButtonMode(bool isOn){
            if (isOn)
            {
                EditorLogic.fetch.partPanelBtn.controlIsEnabled = true;
                EditorLogic.fetch.actionPanelBtn.controlIsEnabled = true;
                EditorLogic.fetch.crewPanelBtn.controlIsEnabled = true;
                EditorLogic.fetch.saveBtn.controlIsEnabled = true;
                EditorLogic.fetch.launchBtn.controlIsEnabled = true;
                EditorLogic.fetch.exitBtn.controlIsEnabled = true;
                EditorLogic.fetch.loadBtn.controlIsEnabled = true;
                EditorLogic.fetch.newBtn.controlIsEnabled = true;
            }
            else
            {
                EditorLogic.fetch.partPanelBtn.controlIsEnabled = true;
                EditorLogic.fetch.actionPanelBtn.controlIsEnabled = true;
                EditorLogic.fetch.crewPanelBtn.controlIsEnabled = true;
                EditorLogic.fetch.saveBtn.controlIsEnabled = false;
                EditorLogic.fetch.launchBtn.controlIsEnabled = false;
                EditorLogic.fetch.exitBtn.controlIsEnabled = false;
                EditorLogic.fetch.loadBtn.controlIsEnabled = true;
                EditorLogic.fetch.newBtn.controlIsEnabled = true;

            }

        }

        public void Update()
        {
            if (this.tabCurrent == 0 && (this.orthoViewRect.width * this.orthoViewRect.height) > 1f)
            {
                this.control.Update((int)this.orthoViewRect.width * 2, (int)this.orthoViewRect.height * 2);
            }
        }
        bool isMouseOver()//https://github.com/m4v/RCSBuildAid/blob/master/Plugin/GUI/MainWindow.cs
        {
            Vector2 position = new Vector2(Input.mousePosition.x,
                                           Screen.height - Input.mousePosition.y);
            return this.windowSize.Contains(position);
        }
        /* Whenever we mouseover our window, we need to lock the editor so we don't pick up
         * parts while dragging the window around */
        void setEditorLock()//https://github.com/m4v/RCSBuildAid/blob/master/Plugin/GUI/MainWindow.cs#L296
        {
            if (visible)
            {
                bool mouseOver = isMouseOver();
                if (mouseOver && !mySoftLock)
                {
                    mySoftLock = true;
                    ControlTypes controlTypes = ControlTypes.CAMERACONTROLS
                                                | ControlTypes.EDITOR_ICON_HOVER
                                                | ControlTypes.EDITOR_ICON_PICK
                                                | ControlTypes.EDITOR_PAD_PICK_PLACE
                                                | ControlTypes.EDITOR_PAD_PICK_COPY
                                                | ControlTypes.EDITOR_EDIT_STAGES
                                                | ControlTypes.EDITOR_GIZMO_TOOLS
                                                | ControlTypes.EDITOR_ROOT_REFLOW;

                    InputLockManager.SetControlLock(controlTypes, this.inputLockIdent);
                }
                else if (!mouseOver && mySoftLock)
                {
                    mySoftLock = false;
                    InputLockManager.RemoveControlLock(this.inputLockIdent);
                }
            }
            else if (mySoftLock)
            {
                mySoftLock = false;
                InputLockManager.RemoveControlLock(this.inputLockIdent);
            }
        }
        public void OnGUI()
        {
            switch (HighLogic.LoadedScene) {//https://github.com/m4v/RCSBuildAid/blob/master/Plugin/GUI/MainWindow.cs
                case GameScenes.EDITOR:
                //case GameScenes.SPH:
                    break;
                default:
                    /* don't show window during scene changes */
                    return;
            }
            if (visible) 
            {
                this.windowSize = GUILayout.Window(GetInstanceID(), this.windowSize, GUIWindow, "Kronal Vessel Viewer", HighLogic.Skin.window);
                //EditorLogic.softLock = this.windowSize.Contains(Event.current.mousePosition);//EditorLogic.softLock not supported anymore? this.windowSize is static not dynamic with drag & drop? what does this do?
            }

            if (Event.current.type == EventType.Repaint)
            {
                setEditorLock();
            }
        }

        private void GUIWindow(int id)
        {
            GUILayout.BeginVertical("box");
            GUIButtons();//draw top buttons
            GUITabShader(this.shaderTabsNames[this.shaderTabCurrent]);//draw shader control buttons
            GUITabView();//show the screenshot preview
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void GUIButtons()
        {
            control.uiBoolVals["saveTextureEvent"] = false;
            if (this.guiStyleButtonAlert == null)
            {
                this.guiStyleButtonAlert = new GUIStyle(GUI.skin.button);
                this.guiStyleButtonAlert.active.textColor = XKCDColors.BrightRed;
                this.guiStyleButtonAlert.hover.textColor = XKCDColors.Red;
                this.guiStyleButtonAlert.normal.textColor = XKCDColors.DarkishRed;
                this.guiStyleButtonAlert.fontStyle = FontStyle.Bold;
                this.guiStyleButtonAlert.fontSize = 8;
                this.guiStyleButtonAlert.stretchWidth = false;
                this.guiStyleButtonAlert.alignment = TextAnchor.MiddleCenter;
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Offset View"))
            {
                this.control.Explode();
            }

            if (GUILayout.Button("Revert"))
            {
                this.control.Config.Revert();
            }

            if (GUILayout.Button("Screenshot"))
            {
                control.uiBoolVals["saveTextureEvent"] = true;
                this.control.Update();
                this.control.Execute();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            if (GUILayout.RepeatButton("ᴖ", GUILayout.Width(34) , GUILayout.Height(34)))
            {
                this.control.direction = Quaternion.AngleAxis(-0.4f, this.control.Camera.transform.right) * this.control.direction;
            }
            if (GUILayout.RepeatButton("ϲ", GUILayout.Width(34) , GUILayout.Height(34)))
            {
                this.control.RotateShip(-1f);
            }
            if (GUILayout.RepeatButton("▲", GUILayout.Width(34) , GUILayout.Height(34)))
            {
                this.control.position.y -= 0.1f;
            }
            if (GUILayout.RepeatButton("ᴐ", GUILayout.Width(34) , GUILayout.Height(34)))
            {
                this.control.RotateShip(1f);
            }
            if (GUILayout.RepeatButton("+", GUILayout.Width(34) , GUILayout.Height(34)))
            {
                this.control.position.z += 0.1f;
            }
            if (GUILayout.Button("RESET", this.guiStyleButtonAlert, GUILayout.Width(34), GUILayout.Height(34)))
            {
                this.control.direction = Vector3.forward;
                this.control.position = Vector3.zero;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.RepeatButton("ᴗ", GUILayout.Width(34) , GUILayout.Height(34)))
            {
                this.control.direction = Quaternion.AngleAxis(0.4f, this.control.Camera.transform.right) * this.control.direction;
            }
            if (GUILayout.RepeatButton("◄", GUILayout.Width(34) , GUILayout.Height(34)))
            {
                this.control.position.x += 0.1f;
            }
            if (GUILayout.RepeatButton("▼", GUILayout.Width(34) , GUILayout.Height(34)))
            {
                this.control.position.y += 0.1f;
            }
            if (GUILayout.RepeatButton("►", GUILayout.Width(34) , GUILayout.Height(34)))
            {
                this.control.position.x -= 0.1f;
            }
            if (GUILayout.RepeatButton("-", GUILayout.Width(34) , GUILayout.Height(34)))
            {
                this.control.position.z -= 0.1f;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            this.control.Orthographic = GUILayout.Toggle(this.control.Orthographic, "Orthographic", GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal();
            this.control.uiFloatVals["shadowVal"] = 0f;
            GUILayout.Label("Shadow", GUILayout.Width(46f));
            GUILayout.Space(3f);
            this.control.uiFloatVals["shadowValPercent"] = GUILayout.HorizontalSlider(this.control.uiFloatVals["shadowValPercent"], 0f, 300f, GUILayout.Width(153f));
            GUILayout.Space(1f);
            GUILayout.Label(this.control.uiFloatVals["shadowValPercent"].ToString("F"), GUILayout.Width(50f));
            this.control.uiFloatVals["shadowVal"] = this.control.uiFloatVals["shadowValPercent"] * 1000f;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("File Quality", GUILayout.Width(68f));
            GUILayout.Space(1f);
            this.control.uiFloatVals["imgPercent"] = GUILayout.HorizontalSlider(this.control.uiFloatVals["imgPercent"]-1, 0f, 8f, GUILayout.Width(140f));
            GUILayout.Space(1f);
            String disW = Math.Floor((control.uiFloatVals["imgPercent"] +1) * control.calculatedWidth).ToString();
            String disH = Math.Floor((control.uiFloatVals["imgPercent"] + 1) * control.calculatedHeight).ToString();
            GUILayout.Label(String.Format("{0:0.#}", this.control.uiFloatVals["imgPercent"].ToString("F")) + "\n" + disW + " x " + disH, GUILayout.Width(110f));
            control.uiFloatVals["imgPercent"] = control.uiFloatVals["imgPercent"] + 1;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            this.shaderTabCurrent = GUILayout.Toolbar(this.shaderTabCurrent, this.shaderTabsNames);
            GUILayout.EndHorizontal();
            
            this.tabCurrent = 0;//used only in Update() be 0.  This will be removed later
        }
        private void GUITabShader(string name)
        {
            if (Array.IndexOf(this.control.Effects.Keys.ToArray<string>(), name) <= -1)//effect not found!
            {
                GUILayout.BeginHorizontal();
                GUITabConfig();
                GUILayout.EndHorizontal();
                return;
            }
            if (name == "Blue Print" && this.control.Effects[name].Enabled)
            {
                GUILayout.BeginHorizontal();
                GUIStyle phaseNotice = new GUIStyle(GUI.skin.label);
                phaseNotice.fontSize = 10;
                phaseNotice.active.textColor = XKCDColors.BrightRed;
                phaseNotice.normal.textColor = XKCDColors.BrightRed;
                GUILayout.Label("**This feature will be phased out. Change Log: 0.0.4**", phaseNotice, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();
            }
            GUILayout.BeginHorizontal();
            this.control.Effects[name].Enabled = GUILayout.Toggle(this.control.Effects[name].Enabled, "Active");
            GUILayout.EndHorizontal();
            for (var i = 0; i < this.control.Effects[name].PropertyCount; ++i)
            {

                if (name == "Blue Print" && !this.control.Effects[name].Enabled)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Background (RGB)", GUILayout.Width(74f));
                    GUILayout.BeginVertical();
                    Color newVal = new Color(1f,1f,1f,1f);
                    newVal.r = GUILayout.HorizontalSlider(this.control.uiFloatVals["bgR"], 0f, 1f);
                    newVal.g = GUILayout.HorizontalSlider(this.control.uiFloatVals["bgG"], 0f, 1f);
                    newVal.b = GUILayout.HorizontalSlider(this.control.uiFloatVals["bgB"], 0f, 1f);
                    this.control.uiFloatVals["bgR"] = newVal.r;
                    this.control.uiFloatVals["bgG"] = newVal.g;
                    this.control.uiFloatVals["bgB"] = newVal.b;
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical();
                    GUILayout.Label(this.control.uiFloatVals["bgR"].ToString("F"), GUILayout.Width(40f));
                    GUILayout.Label(this.control.uiFloatVals["bgG"].ToString("F"), GUILayout.Width(40f));
                    GUILayout.Label(this.control.uiFloatVals["bgB"].ToString("F"), GUILayout.Width(40f));
                    GUILayout.EndVertical();
                    if (GUILayout.Button("RESET", this.guiStyleButtonAlert))
                    {
                        this.control.uiFloatVals["bgR"] = this.control.uiFloatVals["bgR_"];
                        this.control.uiFloatVals["bgG"] = this.control.uiFloatVals["bgG_"];
                        this.control.uiFloatVals["bgB"] = this.control.uiFloatVals["bgB_"];
                    }
                    GUILayout.EndHorizontal();
                    break;
                }
                var prop = this.control.Effects[name][i];
                prop.Match(
                    IfFloat: (p) =>
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(p.DisplayName, GUILayout.Width(60f));
                        p.Value = GUILayout.HorizontalSlider(p.Value, p.RangeMin, p.RangeMax);
                        GUILayout.Label(p.Value.ToString("F"), GUILayout.Width(30f));
                        if (GUILayout.Button("RESET", this.guiStyleButtonAlert)) p.Value = p.DefaultValue;
                        GUILayout.EndHorizontal();
                        GUILayout.Space(2f);
                    },
                    IfColor: (p) =>
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(p.DisplayName, GUILayout.Width(60f));
                        GUILayout.BeginVertical();
                        Color oldVal = p.Value, newVal;
                        newVal.r = GUILayout.HorizontalSlider(oldVal.r, 0f, 1f);
                        newVal.g = GUILayout.HorizontalSlider(oldVal.g, 0f, 1f);
                        newVal.b = GUILayout.HorizontalSlider(oldVal.b, 0f, 1f);
                        newVal.a = 1f;
                        if (newVal != oldVal) p.Value = newVal;
                        GUILayout.EndVertical();
                        GUILayout.BeginVertical();
                        GUILayout.Label(oldVal.r.ToString("F"), GUILayout.Width(40f));
                        GUILayout.Label(oldVal.g.ToString("F"), GUILayout.Width(40f));
                        GUILayout.Label(oldVal.b.ToString("F"), GUILayout.Width(40f));
                        GUILayout.EndVertical();
                        if (GUILayout.Button("RESET", this.guiStyleButtonAlert)) p.Value = p.DefaultValue;
                        GUILayout.EndHorizontal();
                        GUILayout.Space(2f);
                    },
                    IfVector: (p) =>
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(p.DisplayName, GUILayout.Width(60f));
                        GUILayout.BeginVertical();
                        Vector4 oldVal = p.Value, newVal;
                        newVal.x = GUILayout.HorizontalSlider(oldVal.x, 0f, 1f);
                        newVal.y = GUILayout.HorizontalSlider(oldVal.y, 0f, 1f);
                        newVal.z = GUILayout.HorizontalSlider(oldVal.z, 0f, 1f);
                        newVal.w = GUILayout.HorizontalSlider(oldVal.w, 0f, 1f);
                        if (newVal != oldVal) p.Value = newVal;
                        GUILayout.EndVertical();
                        GUILayout.BeginVertical();
                        GUILayout.Label(oldVal.x.ToString("F"), GUILayout.Width(40f));
                        GUILayout.Label(oldVal.y.ToString("F"), GUILayout.Width(40f));
                        GUILayout.Label(oldVal.z.ToString("F"), GUILayout.Width(40f));
                        GUILayout.Label(oldVal.w.ToString("F"), GUILayout.Width(40f));
                        GUILayout.EndVertical();
                        if (GUILayout.Button("RESET", this.guiStyleButtonAlert)) p.Value = p.DefaultValue;
                        GUILayout.EndHorizontal();
                        GUILayout.Space(2f);
                    });
            }
        }

        private void GUITabView()
        {
            GUILayout.BeginVertical();
            control.uiBoolVals["canPreview"] = GUILayout.Toggle(control.uiBoolVals["canPreview"], "Auto-Preview", GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            var r = GUILayoutUtility.GetRect(0, this.windowSize.width, 0, this.windowSize.height);
            if (Event.current.type == EventType.Repaint)
            {
                this.orthoViewRect = r;
            }
            var texture = this.control.Texture();
            if (texture)
            {
                GUI.DrawTexture(this.orthoViewRect, texture, ScaleMode.ScaleToFit, false);
            }
        }

        private void GUITabConfig()
        {
            this.windowScrollPos = GUILayout.BeginScrollView(this.windowScrollPos, false, true);
            foreach (var ol in this.control.Config.Config)
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label("<b>" + ol.Name + "</b>", "box");
                foreach (var o in ol.Options)
                {
                    GUILayout.BeginHorizontal();
                    if (o.IsToggle)
                    {
                        o.valueActive = GUILayout.Toggle(o.valueActive, o.Name);
                    }
                    else
                    {
                        GUILayout.Label(o.Name);
                    }
                    if (o.HasParam)
                    {
                        var displayText = o.valueParam.ToString(o.valueFormat);
                        displayText = GUILayout.TextField(displayText);
                        float value;
                        if (float.TryParse(displayText, out value))
                        {
                            o.valueParam = value;
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
                GUILayout.Space(5);
            }
            GUILayout.EndScrollView();
        }

        void OnGUIAppLauncherReady()
        {
            if (ApplicationLauncher.Ready)
            {
                KVVButton = ApplicationLauncher.Instance.AddModApplication(
                    onAppLaunchToggleOn,
                    onAppLaunchToggleOff,
                    DummyVoid,
                    DummyVoid,
                    DummyVoid,
                    DummyVoid,
                    ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB,
                    (Texture)GameDatabase.Instance.GetTexture("KronalUtils/Textures/icon_button", false));
                control.setFacility();

            }
        }

        void onAppLaunchToggleOn()
        {
            this.axis = EditorLogic.fetch.editorCamera.gameObject.AddComponent<KRSEditorAxis>();
            this.control.UpdateShipBounds();
            visible = true;
        }

        void onAppLaunchToggleOff()
        {
            EditorLogic.DestroyObject(this.axis);
            visible = false;
        }

        void DummyVoid() { }

        void OnDestroy()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);
            if (this.axis != null)
                EditorLogic.DestroyObject(this.axis);

            if (KVVButton != null)
                ApplicationLauncher.Instance.RemoveModApplication(KVVButton);

            Resources.UnloadUnusedAssets();//fix memory leak?
        }
    }
}
