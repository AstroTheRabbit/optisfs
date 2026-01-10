using SFS.UI.ModGUI;
using UITools;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine.UI;

namespace OptiSFS
{
    public class HUD : MonoBehaviour
    {
        private Window win;
        private Label label;

        private float[] frameTimes = new float[30];

        public static int frameIndex;

        public static Dictionary<string, double> times = new Dictionary<string, double>();
        
        public static Dictionary<string, int> frameIndexes = new Dictionary<string, int>();
        
        void Start()
        {
            if (!Entrypoint.DevelopmentMode) return;
            
            Application.runInBackground = true;
            
            Transform holder = Builder.CreateHolder(Builder.SceneToAttach.BaseScene, "FPS HUD").transform;
            
            win = Builder.CreateWindow(holder, Builder.GetRandomID(), 480, 720, draggable: true, titleText: "OptiSFS Benchmark");
            win.CreateLayoutGroup(Type.Vertical, TextAnchor.UpperCenter, padding: new RectOffset(0, 0, 16, 0));
            win.RegisterPermanentSaving("moe.verdix.optisim_HUD");
            win.EnableScrolling(Type.Vertical);
            
            frameTimes[frameIndex % frameTimes.Length] = Time.unscaledDeltaTime;
            
            label = Builder.CreateLabel(win, 420, 2000, text: "");
            var csf = label.gameObject.GetOrAddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            //csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            label.TextAlignment = TextAlignmentOptions.TopLeft;
            label.AutoFontResize = false;
            label.FontSize = 32;
        }

        void Update()
        {
            frameIndex++;
            
            if (!Entrypoint.DevelopmentMode) return;
            
            if (Input.GetKeyDown(KeyCode.Backslash))
                Entrypoint.PatchEnabled ^= true;
            if (Input.GetKeyDown(KeyCode.Home))
                Entrypoint.TreatmentGroup ^= true;

            frameTimes[frameIndex % frameTimes.Length] = Time.unscaledDeltaTime;
            
            StringBuilder sb = new StringBuilder();

            foreach (var key in times.Keys)
            {
                sb.AppendLine($"{key}: {times[key].Round(3)}ms");
            }
            
            label.Text = $"FPS: {frameTimes.Select(dt => 1/(dt == 0 ? 1 : dt)).Average().Round(1)}\nPatch {(Entrypoint.PatchEnabled ? "ON" : "OFF")}\n{sb}";
        }
    }
}