using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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
        public string valueFormat;

        //constructor
        public VesselElementViewOption(string name, bool isToggle, bool hasParam,
            Action<VesselElementViewOptions, VesselElementViewOption, Part> apply,
            bool defaultValueActive = false, float defaultValueParam = 0f,
            string valueFormat = "F2")
        {
            this.Name = name;
            this.IsToggle = isToggle;
            this.HasParam = hasParam;
            this.Apply = apply;
            this.valueActive = defaultValueActive;
            this.valueParam = defaultValueParam;
            this.valueFormat = valueFormat;
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
            if (!this.CanApply(part)) return;

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
//UnityEngine.Debug.Log(string.Format("KVV: name: {0}", name));//future debubging
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
                        new VesselElementViewOption("Offset", true, true, EngineFairingExplode, true, 1f),
                        new VesselElementViewOption("Hide", true, false, EngineFairingHide, true),
                    }
                }
            };
            if(hasMod("KAS")){
                Config.Add(new VesselElementViewOptions("KAS Connector Ports", CanApplyIfModule("KASModulePort")) {
                    Options = {
                        new VesselElementViewOption("Offset", true, true, KASConnectorPortExplode, true, 1f),
                    }
                });
            }
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
            
            Config.Add(new VesselElementViewOptions("Struts", CanApplyIfType("StrutConnector")) {
                    Options = {
                        new VesselElementViewOption("Hide", true, false, PartHideRecursive, true),
                    }
            });
            Config.Add(new VesselElementViewOptions("Launch Clamps", CanApplyIfModule("LaunchClamp")) {
                    Options = {
                        new VesselElementViewOption("Hide", true, false, PartHideRecursive, true),
                    }
             });
        }

        //updated for simpflication
        private void StateToggle(bool toggleOn)
        {
            //var p = EditorLogic.startPod;
            var p = EditorLogic.RootPart;
            if (toggleOn)
            {
                this.positions.Clear();
                this.visibility.Clear();
                this.freezed.Clear();
                this.procFairings.Clear();
            }
            foreach (var t in p.GetComponentsInChildren<Transform>()){
                if (toggleOn) { this.positions[t] = t.localPosition; }
                else if ((!toggleOn) && this.positions.ContainsKey(t)) { t.localPosition = this.positions[t]; }
            }
            foreach (var r in p.GetComponentsInChildren<Renderer>())
            {
                if (toggleOn) { this.visibility[r] = r.enabled; }
                else if ((!toggleOn) && this.visibility.ContainsKey(r)) { r.enabled = this.visibility[r]; }
            }
            foreach (var part in this.ship.Parts)
            {
                if (toggleOn) { 
                    this.freezed[part] = part.frozen; 
                }
                else if ((!toggleOn) && this.freezed.ContainsKey(part)) { 
                    part.frozen = this.freezed[part]; 
                }

                if (hasMod("ProceduralFairings"))
                {
                    this.proceduralFairingToggleState(toggleOn, part);
                }
            }
            if (!toggleOn) { this.onRevert(); }
            //else { this.onSaveState(); }
        }

        //apply locked state?
        private void SaveState()
        {
            this.StateToggle(true);
        }

        //apply locked state?
        public void Revert()
        {
            this.StateToggle(false);
        }

        public void Execute(IShipconstruct ship)
        {
            this.ship = ship;
            StateToggle(false);//Revert();
            StateToggle(true);//SaveState();
            foreach (var part in ship.Parts)
            {
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
        }

        private Func<Part, bool> CanApplyIfType(string typeName)
        {
            var type = KRSUtils.FindType(typeName);
            return (p) => type.IsInstanceOfType(p);
        }

        private Func<Part, bool> CanApplyIfModule(string moduleName)
        {
            return (p) => p.Modules.Contains(moduleName);
        }

        private void PartHide(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            //MonoBehaviour.print("Hiding Part " + part.ToString());
            foreach (var r in part.GetComponents<Renderer>())
            {
                r.enabled = false;
            }
        }

        private void PartHideRecursive(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            //MonoBehaviour.print("Hiding Part " + part.ToString());
            foreach (var r in part.GetComponentsInChildren<Renderer>())
            {
                r.enabled = false;
            }
        }

        private void StackDecouplerExplode(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            //MonoBehaviour.print("Exploding Stack Decoupler: " + part.ToString());
            var module = part.Module<ModuleDecouple>();
            if (module.isDecoupled) return;
            if (!module.staged) return; // don't explode if tweakable staging is false
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
            //MonoBehaviour.print("Exploding Radial Decoupler: " + part.ToString());
            var module = part.Module<ModuleAnchoredDecoupler>();
            if (module.isDecoupled) return;
            if (!module.staged) return; // don't explode if tweakable staging is false
            if (string.IsNullOrEmpty(module.explosiveNodeID)) return;
            var an = module.explosiveNodeID == "srf" ? part.srfAttachNode : part.findAttachNode(module.explosiveNodeID);
            if (an == null || an.attachedPart == null) return;
            var distance = o.valueParam;
            if (part.name.Contains("FairingCone"))
            {
                distance *= -1; // invert distance for KW Fairings.
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
            //MonoBehaviour.print("Exploding Docking Port: " + part.ToString());
            var module = part.Module<ModuleDockingNode>();
            if (string.IsNullOrEmpty(module.referenceAttachNode)) return;
            var an = part.findAttachNode(module.referenceAttachNode);
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
            //MonoBehaviour.print("Exploding Engine Fairing: " + part.ToString());
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
            //MonoBehaviour.print("Hiding Engine Fairing: " + part.ToString());
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
            if (hasMod("KAS"))
            {
                var module = part.Module<KAS.KASModulePort>();//this creates KAS Dependancy.  
                if (string.IsNullOrEmpty(module.attachNode)) return;
                var an = part.findAttachNode(module.attachNode);
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
        }

        private void ProcFairingExplode(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            if (hasMod("ProceduralFairings"))
            {
                //MonoBehaviour.print("Exploding Procedural Fairing: " + part.ToString());
                var nct = part.FindModelTransform("nose_collider");
                //Debug.Log(string.Format("KVV: ProcFairingExplode {0}", nct.ToString()));
                if (!nct) return;
                this.procFairingOffset = o.valueParam; // steal the offset value. to be added to vessel width for rendering.
                //MeshFilter mf;
                //Vector3 extents = (mf = part.gameObject.GetComponentInChildren<MeshFilter>()) ? mf.mesh.bounds.size : new Vector3(o.valueParam, o.valueParam, o.valueParam); // original
                Vector3 extents = new Vector3(o.valueParam, o.valueParam, o.valueParam);
                part.transform.Translate(Vector3.Scale(nct.right, extents), Space.World);
            }
        }

        private void ProcFairingHide(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            if (hasMod("ProceduralFairings"))
            {
                //MonoBehaviour.print("Hiding Procedural Fairing: " + part.ToString());
                var nct = part.FindModelTransform("nose_collider");
                if (!nct) return;
                //var forward = EditorLogic.startPod.transform.forward;
                //var right = EditorLogic.startPod.transform.right;
                var forward = EditorLogic.RootPart.transform.forward;
                var right = EditorLogic.RootPart.transform.right;
                
                //if (Vector3.Dot(nct.right, -(forward + right).normalized) > 0f) // original
                if (Vector3.Dot(nct.right, -(forward).normalized) > 0f)
                {
                    var renderer = part.GetComponentInChildren<Renderer>();
                    if (renderer) renderer.enabled = false;
                }
            }
        }
    }
}
