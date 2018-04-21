using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KronalUtils
{
    class KRSEditorAxis: MonoBehaviour
    {
        public Material mat;
        public EditorVesselOverlays evo;

        
        void CreateLineMaterial()
        {
            if (!mat)
            {
                mat = new Material("Shader \"Lines/Colored Blended\" {" +
                    "SubShader { Pass { " +
                    "    Blend SrcAlpha OneMinusSrcAlpha " +
                    "    ZWrite Off ZTest Always Cull Off Fog { Mode Off } " +
                    "    BindChannels {" +
                    "      Bind \"vertex\", vertex Bind \"color\", color }" +
                    "} } }");
                mat.hideFlags = HideFlags.HideAndDontSave;
                mat.shader.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        void Awake()
        {
            CreateLineMaterial();
            this.evo = (EditorVesselOverlays) GameObject.FindObjectOfType(typeof(EditorVesselOverlays));
            print("awake");
        }

        void OnPostRender()
        {
            if (!this.evo.CoMmarker.gameObject.activeInHierarchy) return;

            GL.PushMatrix();
            mat.SetPass(0);
            GL.Begin(GL.LINES);
            var t = this.evo.CoMmarker.posMarkerObject.transform;
            Vector3 dirInterval;
            int dirAxis;
            dirAxis = -Math.Sign(Vector3.Dot(this.camera.transform.forward, t.forward));

            GL.Color(Color.yellow * new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, (Math.Abs(Vector3.Dot(this.camera.transform.forward, t.forward)) - 0.707f) * 5f)));
            GL.Vertex(t.position - t.forward * 10f);
            GL.Vertex(t.position + t.forward * 10f);
            dirInterval = (Math.Abs(Vector3.Dot(this.camera.transform.forward, t.right)) < 0.7071 ? t.right : t.up) * 0.5f;
            for (var i = 0; i < 10; ++i)
            {
                GL.Vertex(t.position - t.forward * i - dirInterval);
                GL.Vertex(t.position - t.forward * i + dirInterval);
                GL.Vertex(t.position + t.forward * i - dirInterval);
                GL.Vertex(t.position + t.forward * i + dirInterval);
            }

            GL.Color(Color.yellow * new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, (Math.Abs(Vector3.Dot(this.camera.transform.forward, t.right)) - 0.707f) * 5f)));
            GL.Vertex(t.position - t.right * 10f);
            GL.Vertex(t.position + t.right * 10f);
            dirInterval = (Math.Abs(Vector3.Dot(this.camera.transform.forward, t.forward)) < 0.7071 ? t.forward : t.up) * 0.5f;
            for (var i = 0; i < 10; ++i)
            {
                GL.Vertex(t.position - t.right * i - dirInterval);
                GL.Vertex(t.position - t.right * i + dirInterval);
                GL.Vertex(t.position + t.right * i - dirInterval);
                GL.Vertex(t.position + t.right * i + dirInterval);
            }

            GL.Color(Color.yellow * new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, (Math.Abs(Vector3.Dot(this.camera.transform.forward, t.up)) - 0.707f) * 5f)));
            GL.Vertex(t.position - t.up * 10f);
            GL.Vertex(t.position + t.up * 10f);
            dirInterval = (Math.Abs(Vector3.Dot(this.camera.transform.forward, t.forward)) < 0.7071 ? t.forward : t.right) * 0.5f;
            for (var i = 0; i < 10; ++i)
            {
                GL.Vertex(t.position - t.up * i - dirInterval);
                GL.Vertex(t.position - t.up * i + dirInterval);
                GL.Vertex(t.position + t.up * i - dirInterval);
                GL.Vertex(t.position + t.up * i + dirInterval);
            }
            GL.End();
            GL.PopMatrix();
        }
    }
}
