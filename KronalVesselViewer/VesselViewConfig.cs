
#define KERAMZIT
#define KAS

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using UnityEngine;
using KAS;

using ProceduralFairings;

namespace KronalUtils
{
    class VesselElementViewOption
    {
        public string Name { get; private set; }
        public bool IsToggle { get; private set; }
        public bool HasParam { get; private set; }
        public Action<VesselElementViewOptions, VesselElementViewOption, Part> Apply { get; private set; }
        public bool valueActive;
        public float valueParam;
        public float minValueParam;
        public float maxValueParam;

        public string valueFormat;

        //constructor
        public VesselElementViewOption(string name, bool isToggle, bool hasParam,
            Action<VesselElementViewOptions, VesselElementViewOption, Part> apply,
            bool defaultValueActive = false, float defaultValueParam = 0f, float minValue = 0f, float maxValue = 5f,
            string valueFormat = "F2")
        {
            this.Name = name;
            this.IsToggle = isToggle;
            this.HasParam = hasParam;
            this.Apply = apply;
            this.valueActive = defaultValueActive;
            this.valueParam = defaultValueParam;
            this.valueFormat = valueFormat;
            this.minValueParam = minValue;
            this.maxValueParam = maxValue;
        }
    }

    class VesselElementViewOptions
    {
        public string Name { get; private set; }
        public Func<Part, bool> CanApply { get; private set; }
        public List<VesselElementViewOption> Options { get; private set; }

        //constructor
        public VesselElementViewOptions(string name, Func<Part, bool> canApply)
        {
            this.Name = name;
            this.CanApply = canApply;
            this.Options = new List<VesselElementViewOption>();
        }


        internal void Apply(Part part)
        {
            if (!this.CanApply(part))
            {
                return;
            }
            if (Options == null)
            {
                return;
            }
            foreach (var option in this.Options)
            {
                if (option.valueActive)
                {
                    option.Apply(this, option, part);
                }
            }
        }
    }

    class VesselViewConfig
    {
        private Dictionary<Transform, Vector3> positions;
        private Dictionary<Renderer, bool> visibility;
        private Dictionary<Part, bool> freezed;
        public Dictionary<Part, bool> procFairings;
        public float procFairingOffset = 0f;
        private IShipconstruct ship;
        public List<VesselElementViewOptions> Config { get; private set; }
        public Action onApply;
        public Action onRevert;
        public List<String> installedMods = new List<String>();
        public void buildModList()
        {
            //https://github.com/Xaiier/Kreeper/blob/master/Kreeper/Kreeper.cs#L92-L94 <- Thanks Xaiier!
            foreach (AssemblyLoader.LoadedAssembly a in AssemblyLoader.loadedAssemblies)
            {
                string name = a.name;
                log.debug(string.Format("Loading assembly: {0}", name));
                installedMods.Add(name);
            }
        }
        public bool hasMod(string modIdent)
        {
            return installedMods.Contains(modIdent);
        }

        //constructor
        public VesselViewConfig()
        {
            log.debug("VesselViewConfig");
            buildModList();
            this.positions = new Dictionary<Transform, Vector3>();
            this.visibility = new Dictionary<Renderer, bool>();
            this.freezed = new Dictionary<Part, bool>();
            this.procFairings = new Dictionary<Part, bool>();
            this.onApply = () => { };
            this.onRevert = () => { };
            this.Config = new List<VesselElementViewOptions>() {
                new VesselElementViewOptions("Stack Decouplers/Separators", CanApplyIfModule("ModuleDecouple")) {
                    Options = {
                        new VesselElementViewOption("Offset", true, true, StackDecouplerExplode, true, 1f),
                    }
                },
                new VesselElementViewOptions("Radial Decouplers/Separators", CanApplyIfModule("ModuleAnchoredDecoupler")) {
                    Options = {
                        new VesselElementViewOption("Offset", true, true, RadialDecouplerExplode, true, 1f),
                    }
                },
                new VesselElementViewOptions("Docking Ports", CanApplyIfModule("ModuleDockingNode")) {
                    Options = {
                        new VesselElementViewOption("Offset", true, true, DockingPortExplode, true, 1f),
                    }
                },
                new VesselElementViewOptions("Engine Fairings", CanApplyIfModule("ModuleJettison")) {
                    Options = {
                       // new VesselElementViewOption("Offset", true, true, EngineFairingExplode, true, 1f),
                        new VesselElementViewOption("Hide", true, false, EngineFairingHide, true),
                    }
                }
            };
#if KAS
            if (hasMod("KAS"))
            {
                Config.Add(new VesselElementViewOptions("KAS Connector Ports", CanApplyIfModule("KASModulePort"))
                {
                    Options = {
                        new VesselElementViewOption("Offset", true, true, KASConnectorPortExplode, true, 1f),
                    }
                });
            }
#endif

            Config.Add(new VesselElementViewOptions("Stock Fairings", CanApplyIfModule("ModuleProceduralFairing"))
            {
                Options = {
                    new VesselElementViewOption("Opacity (0 = opaque, 1 = solid)", false, true, StockProcFairingSetOpacity, true, 1f, 0f, 1f),
                    new VesselElementViewOption("Offset", true, true, StockProcFairingExplode, true, 1f, 0.1f, 1f),
                    new VesselElementViewOption("Hide", true, false, StockProcFairingHide, false),
                }
            });

#if KERAMZIT
            if (hasMod("ProceduralFairings"))
            {
                Config.Add(new VesselElementViewOptions("Procedural Fairings", CanApplyIfModule("ProceduralFairingSide"))
                {
                    Options = {
                        new VesselElementViewOption("Offset", true, true, ProcFairingExplode, false, 3f),
                        new VesselElementViewOption("Hide", true, false, PartHideRecursive, false),
                        new VesselElementViewOption("Hide front half", true, false, ProcFairingHide, false),
                    }
                });
            }
#endif
#if false
            Config.Add(new VesselElementViewOptions("Struts", CanApplyIfType("StrutConnector")) {
                    Options = {
                        new VesselElementViewOption("Hide", true, false, PartHideRecursive, true),
                    }
            });
#endif
            Config.Add(new VesselElementViewOptions("Struts", CanApplyIfModule("CModuleStrut"))
            {
                Options = {
                        new VesselElementViewOption("Hide", true, false, PartHideRecursive, true),
                    }
            });
            Config.Add(new VesselElementViewOptions("Launch Clamps", CanApplyIfModule("LaunchClamp"))
            {
                Options = {
                        new VesselElementViewOption("Hide", true, false, PartHideRecursive, true),
                    }
            });
            log.debug(string.Format("Config list contains: {0}", Config.Count));
        }

        bool curState = false;
        //updated for simpflication
        private void StateToggle(bool toggleOn)
        {
            if (toggleOn == curState)
                return;

            var p = EditorLogic.RootPart;
            if (toggleOn)
            {
                this.positions.Clear();
                this.visibility.Clear();
                this.freezed.Clear();
                this.procFairings.Clear();
            }
            if (p != null)
            {
                foreach (var t in p.GetComponentsInChildren<Transform>())
                {
                    if (toggleOn) { this.positions[t] = t.localPosition; }
                    else if ((!toggleOn) && this.positions.ContainsKey(t)) { t.localPosition = this.positions[t]; }
                }
                foreach (var r in p.GetComponentsInChildren<Renderer>())
                {
                    if (toggleOn) { this.visibility[r] = r.enabled; }
                    else if ((!toggleOn) && this.visibility.ContainsKey(r)) { r.enabled = this.visibility[r]; }
                }
            }
            if (ship != null && ship.Parts != null)
            {
                foreach (var part in this.ship.Parts)
                {
                    if (toggleOn)
                    {
                        this.freezed[part] = part.frozen;
                    }
                    else if ((!toggleOn) && this.freezed.ContainsKey(part))
                    {
                        part.frozen = this.freezed[part];
                    }

                    if (hasMod("ProceduralFairings"))
                    {
                        this.proceduralFairingToggleState(toggleOn, part);
                    }
                    if (!toggleOn)
                        StockProceduralFairingToggleState(toggleOn, part);
                }
            }
            if (!toggleOn) { this.onRevert(); }
            curState = toggleOn;
            //else { this.onSaveState(); }
        }

        private void SaveState()
        {
            this.StateToggle(true);
        }

        public void Revert()
        {
            this.StateToggle(false);
        }

        public void Execute(IShipconstruct ship)
        {
            this.ship = ship;
            if (curState)
                StateToggle(false);//Revert();
            StateToggle(true);//SaveState();

            foreach (var part in ship.Parts)
            {
                log.debug(string.Format("Execute, part: {0}", part.partInfo.title));
                if (part.Modules.Contains("ModuleProceduralFairing"))
                {
                    StockProcFairing_st_idle_replacement(true, part);
                }
                foreach (var c in this.Config)
                {

                    c.Apply(part);
                }
                part.frozen = true;
            }

            this.onApply();
        }


        private void proceduralFairingToggleState(Boolean toggleOn, Part part)
        {
#if KERAMZIT
            if (part.Modules.Contains("ProceduralFairingSide") && hasMod("ProceduralFairings"))
            {
                var module = part.Module<Keramzit.ProceduralFairingSide>();
        
                // Preserve ship's original fairing lock state.
                if (toggleOn && !module.shapeLock)
                {
                    module.shapeLock = true;
                    this.procFairings[part] = true;
                }
                else if (!toggleOn && this.procFairings.ContainsKey(part))
                {
                    module.shapeLock = false;
                }
            }
#endif
        }

        KerbalFSM fsm;
        KFSMState saved_st_editor_idle = null;
        KFSMState replacement_st_editor_idle = null;
        private void StockProcFairing_st_idle_replacement(bool save, Part part)
        {
            var module = part.Module<ModuleProceduralFairing>();

            MethodInfo[] m = part.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            FieldInfo[] fields = module.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (replacement_st_editor_idle == null)
            {
                replacement_st_editor_idle = new KFSMState("st_idle");
                replacement_st_editor_idle.OnLateUpdate = delegate
                {
                    MouseFadeUpdate();
                };
            }

            foreach (FieldInfo fi in fields)
            {
                if (fi.Name == "fsm")
                {
                    fsm = (KerbalFSM)fi.GetValue(module);
                    FieldInfo[] fsmFields = fsm.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (FieldInfo fsmFi in fsmFields)
                    {
                        if (fsmFi.Name == "States")
                        {
                            List<KFSMState> States = (List<KFSMState>)fsmFi.GetValue(fsm);

                            foreach (var s in States)
                            {
                                log.debug(string.Format("KFSMState name: {0}", s.name));
                                if (s.name == "st_idle")
                                {
                                    if (save)
                                    {
                                        saved_st_editor_idle = s;
                                        States.Remove(s);
                                        fsm.AddState(replacement_st_editor_idle);
                                    }
                                    else
                                    {
                                        States.Remove(s);
                                        fsm.AddState(saved_st_editor_idle);
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    fsm.StartFSM("st_idle");
                    if (!save)
                    {
                        foreach (var p in module.Panels)
                        {
                            p.SetExplodedView(0);
                            p.SetOpacity(1);
                        }
                    }
                    break;
                }

            }
        }

        void MouseFadeUpdate()
        {
            // A do-nothing function, needed so that the st_idle can be replaced above

        }


        private void StockProceduralFairingToggleState(Boolean toggleOn, Part part)
        {
            log.debug("StockProceduralFairingToggleState");

            if (part.Modules.Contains("ModuleProceduralFairing"))
            {

                StockProcFairing_st_idle_replacement(toggleOn, part);

                var module = part.Module<ModuleProceduralFairing>();

                foreach (var p in module.Panels)
                    p.go.SetActive(!toggleOn);
            }
            fairingPanels = null;
        }

        private Func<Part, bool> CanApplyIfType(string typeName)
        {
            var type = KVrUtils.FindType(typeName);
            return (p) => type.IsInstanceOfType(p);
        }

        private Func<Part, bool> CanApplyIfModule(string moduleName)
        {
            return (p) => p.Modules.Contains(moduleName);
        }

        private void PartHide(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            log.debug(string.Format("Hiding Part {0}", part));

            foreach (var r in part.GetComponents<Renderer>())
            {
                r.enabled = false;
            }
        }

        private void PartHideRecursive(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            log.debug(string.Format("Hiding Part {0}", part));

            foreach (var r in part.GetComponentsInChildren<Renderer>())
            {
                r.enabled = false;
            }
        }

        private void StackDecouplerExplode(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            log.debug(string.Format("Exploding Stack Decoupler: {0}", part));

            var module = part.Module<ModuleDecouple>();
            if (module.isDecoupled) return;
            if (!module.staged) return;
            if (!part.parent) return;
            Vector3 dir;
            if (module.isOmniDecoupler)
            {
                foreach (var c in part.children)
                {
                    dir = Vector3.Normalize(c.transform.position - part.transform.position);
                    c.transform.Translate(dir * o.valueParam, Space.World);
                }
            }
            dir = Vector3.Normalize(part.transform.position - part.parent.transform.position);
            part.transform.Translate(dir * o.valueParam, Space.World);
        }


        private void RadialDecouplerExplode(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            log.debug(string.Format("Exploding Radial Decoupler: {0}", part));

            var module = part.Module<ModuleAnchoredDecoupler>();
            if (module.isDecoupled) return;
            if (!module.staged) return;
            if (string.IsNullOrEmpty(module.explosiveNodeID)) return;
            var an = module.explosiveNodeID == "srf" ? part.srfAttachNode : part.FindAttachNode(module.explosiveNodeID);
            if (an == null || an.attachedPart == null) return;
            var distance = o.valueParam;
            if (part.name.Contains("FairingCone"))
            {
                distance *= -1;
            }
            Part partToBeMoved;
            if (an.attachedPart == part.parent)
            {
                distance *= -1;
                partToBeMoved = part;
            }
            else
            {
                partToBeMoved = an.attachedPart;
            }
            partToBeMoved.transform.Translate(part.transform.right * distance, Space.World);
        }

        private void DockingPortExplode(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            log.debug(string.Format("Exploding Docking Port: {0}", part));

            var module = part.Module<ModuleDockingNode>();
            if (string.IsNullOrEmpty(module.referenceAttachNode)) return;
            var an = part.FindAttachNode(module.referenceAttachNode);
            if (!an.attachedPart) return;
            var distance = o.valueParam;
            Part partToBeMoved;
            if (an.attachedPart == part.parent)
            {
                distance *= -1;
                partToBeMoved = part;
            }
            else
            {
                partToBeMoved = an.attachedPart;
            }
            partToBeMoved.transform.Translate(module.nodeTransform.forward * distance, Space.World);
        }

        private void EngineFairingExplode(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            log.debug(string.Format("Exploding Engine Fairing: {0}", part));

            var module = part.Module<ModuleJettison>();
            if (!module.isJettisoned)
            {
                if (!module.isFairing)
                {
                    module.jettisonTransform.Translate(module.jettisonDirection * o.valueParam);
                }
            }
        }

        private void EngineFairingHide(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            log.debug(string.Format("Hiding Engine Fairing: {0}", part));

            var module = part.Module<ModuleJettison>();
            if (module.jettisonTransform)
            {
                foreach (var r in module.jettisonTransform.gameObject.GetComponentsInChildren<Renderer>())
                {
                    r.enabled = false;
                }
            }
        }

        private void KASConnectorPortExplode(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
#if KAS
            if (hasMod("KAS"))
            {
                var module = part.Module<KAS.KASModulePort>();//this creates KAS Dependancy.  
                if (string.IsNullOrEmpty(module.attachNode)) return;
                var an = part.FindAttachNode(module.attachNode);
                if (!an.attachedPart) return;
                var distance = o.valueParam;
                Part partToBeMoved;
                if (an.attachedPart == part.parent)
                {
                    distance *= -1;
                    partToBeMoved = part;
                }
                else
                {
                    partToBeMoved = an.attachedPart;
                }
                partToBeMoved.transform.Translate(module.portNode.forward * distance, Space.World);
            }
#endif
        }

        float opacity = 1f;
        private void StockProcFairingSetOpacity(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            opacity = o.valueParam;
        }

        public static List<ProceduralFairings.FairingPanel> fairingPanels = null;
        public static float fairingPanelValueParam;
        private void StockProcFairingExplode(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            log.debug(string.Format("Exploding Procedural Fairing: {0}", part));
            fairingPanels = new List<ProceduralFairings.FairingPanel>();
            var module = part.Module<ModuleProceduralFairing>();
            fairingPanelValueParam = o.valueParam;
            foreach (var p in module.Panels)
            {
                fairingPanels.Add(p);
                p.SetExplodedView(VesselViewConfig.fairingPanelValueParam);
                //p.SetTgtExplodedView(VesselViewConfig.fairingPanelValueParam);
                p.SetOpacity(opacity);
                //p.SetTgtOpacity(0);
            }
        }

        private void StockProcFairingHide(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            log.debug(string.Format("Hiding StockProcFairingHide Fairing: {0}", part));
            var module = part.Module<ModuleProceduralFairing>();

            foreach (var p in module.Panels)
                p.go.SetActive(false);
        }

        private void ProcFairingExplode(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            if (hasMod("ProceduralFairings"))
            {
                log.debug(string.Format("Exploding Procedural Fairing: {0}", part));
                var nct = part.FindModelTransform("nose_collider");
                log.debug(string.Format("ProcFairingExplode {0}", nct));

                if (!nct) return;
                this.procFairingOffset = o.valueParam;
                Vector3 extents = new Vector3(o.valueParam, o.valueParam, o.valueParam);
                part.transform.Translate(Vector3.Scale(nct.right, extents), Space.World);
            }
        }

        private void ProcFairingHide(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            if (hasMod("ProceduralFairings"))
            {
                log.debug(string.Format("Hiding Procedural Fairing: {0}", part));
                var nct = part.FindModelTransform("nose_collider");
                if (!nct) return;
                //var forward = EditorLogic.startPod.transform.forward;
                //var right = EditorLogic.startPod.transform.right;

                var forward = EditorLogic.RootPart.transform.forward;
                var right = EditorLogic.RootPart.transform.right;

                if (Vector3.Dot(nct.right, -(forward).normalized) > 0f)
                {
                    var renderer = part.GetComponentInChildren<Renderer>();
                    if (renderer) renderer.enabled = false;
                }
            }
        }
    }
}
