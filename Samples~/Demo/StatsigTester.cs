using System.Collections.Generic;
using StatsigUnity;
using TMPro;
using UnityEngine;

namespace StatsigUnitySamples.Demo
{
    public class StatsigTester : MonoBehaviour
    {
        public TMP_InputField sdkKeyInput;
        public TMP_InputField userIDInput;
        public TMP_InputField gateNameInput;
        public TMP_InputField configNameInput;
        public TMP_InputField experimentNameInput;
        public TMP_InputField layerNameInput;
        public TMP_InputField layerParamInput;
        public TMP_InputField logEventInput;
        public TMP_Text statusLabel;
        public List<string> statuses;

        public async void OnInitializedClicked()
        {
            Debug.Log("Init with " + sdkKeyInput.text);
            Debug.Log("User is " + userIDInput.text);
            await Statsig.Initialize(sdkKeyInput.text, new StatsigUser
            {
                UserID = userIDInput.text
            }, new StatsigOptions()
            {
                // ApiUrlBase = "http://localhost:3006/v1"
            });

            AddStatus("Statsig Initialized");
        }

        public async void OnShutdownClicked()
        {
            await Statsig.Shutdown();
            AddStatus("Statsig Shutdown");
        }

        public void OnCheckGateClicked()
        {
            var result = Statsig.CheckGate(gateNameInput.text);
            AddStatus($"Checked Gate {gateNameInput.text} -- {(result ? "True" : "False")}");
        }

        public void OnGetConfigClicked()
        {
            Debug.Log("Get Config " + configNameInput.text);

            var result = Statsig.GetConfig(configNameInput.text);
            AddStatus($"  Value: {string.Join(" ", result.Value)}");
            AddStatus($"Get Config {configNameInput.text}");
        }

        public void OnGetExperimentClicked()
        {
            var result = Statsig.GetExperiment(experimentNameInput.text);
            
            AddStatus($"  Rule: {result.RuleID}");
            AddStatus($"  Value: {string.Join(" ", result.Value)}");
            AddStatus($"Get Experiment {experimentNameInput.text}");
        }

        public void OnGetLayerClicked()
        {
            var result = Statsig.GetLayer(layerNameInput.text);
            
            AddStatus($"  Rule: {result.RuleID}");
            AddStatus($"  Value: {string.Join(" ", result.Get(layerParamInput.text, "fallback"))}");
            AddStatus($"Get Layer {layerNameInput.text}");
        }

        public void OnLogEventClicked()
        {
            Statsig.LogEvent(logEventInput.text, 1, new Dictionary<string, string> { { "Foo", "Bar" } });
            AddStatus($"Event Logged: {logEventInput.text}");
        }


        private void AddStatus(string status)
        {
            statuses.Insert(0, status);
            if (statuses.Count > 10)
            {
                statuses.RemoveAt(9);
            }

            statusLabel.text = string.Join("\n", statuses);
        }
    }
}