﻿using Assets.Scripts.Services;
using Assets.Scripts.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.DayNightCycle
{
    public class DayAndNightControl : MonoBehaviour
    {

        public GameObject Camera;
        public GameObject moon;
        public float sunTilt = -15f;
        [SerializeField] private TimecycleProfile timecycle = null;
        [SerializeField] private float sunRotationOffset = 0f;
        [Tooltip("The amount of frames to wait before doing the next lighting update")]
        [SerializeField] private int lightingUpdateInterval = 10;
        public Material skyBoxPROCEDURAL;

        [Range(0f, 24f)] public float currentTime;
        public float CurrentTime01 { get { return currentTime / 24; } set { currentTime = value * 24; } }
        public Slider TimeSlider;
        public Camera MoonCamera;
        public GameObject DayNightController;
        public int currentDay = 0;
        public Light directionalLight;
        public float SecondsInAFullDay = 500f;//default value is 500 seconds in one day(this seems to not correlate correctly to 1f to 1second, TODO fix
        public bool pause { get; set; }
        
        [HideInInspector]
        public float timeMultiplier = 1f; //how fast the day goes by regardless of the secondsInAFullDay var. lower values will make the days go by longer, while higher values make it go faster. This may be useful if you're siumulating seasons where daylight and night times are altered.
        float lightIntensity; //static variable to see what the current light's insensity is in the inspector
        public GameObject  SettingsUI = null;

        Camera targetCam;
        private int frames;

       
        // Use this for initialization
        void Start()
        {
            pause=true;
            RenderSettings.skybox = skyBoxPROCEDURAL;
            SettingsUI = GameObject.Find("Game Settings");
            Service.Settings.OnTimeSettingsChanged += Settings_OnTimeSettingsChanged;
            MoonCamera = GetComponentInChildren<Camera>();
            TimeSlider = GameObject.Find("TimeSlider").GetComponent<Slider>();
            foreach (Camera c in GameObject.FindObjectsOfType<Camera>())
            {
                if (c.isActiveAndEnabled)
                {
                    targetCam = c;
                }
            }
            lightIntensity = directionalLight.intensity; //what's the current intensity of the light
            // AFAIK procedural skybox needs this to work
            RenderSettings.sun = directionalLight;


            //Check if default light prefab exists, and if so, disable it
            if (GameObject.Find("LightSet"))
            {
                GameObject.Find("LightSet").SetActive(false);
            }

            if (timecycle)
            {
                if (timecycle.overrideEnvironmentLighting)
                {
                    switch (timecycle.lightingOverrideMode)
                    {
                        case TimecycleProfile.AmbientLightingOverrideMode.Gradient:
                            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                            break;
                        case TimecycleProfile.AmbientLightingOverrideMode.Color:
                            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                            break;
                    }
                }
                else
                {
                    RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
                }
            }
            SettingsUI.SetActive(false);

            targetCam = GameObject.Find("MainCamera").GetComponent<Camera>();
        }

        private void Settings_OnTimeSettingsChanged(TimeSettings settings)
        {
            currentTime = (float) GameSettings.Time.currentTime;
            SecondsInAFullDay = (float) GameSettings.Time.dayLength;
            pause = (bool) GameSettings.Time.pause;
            
        }

        private void OnDestroy()
        {
            Service.Settings.OnTimeSettingsChanged -= Settings_OnTimeSettingsChanged;
            SettingsUI.SetActive(true);
        }
        // Update is called once per frame
        void Update()
        {
            if (GameObject.Find("LightSet"))
            {
                GameObject.Find("LightSet").SetActive(false);
            }
            //updates timeslider when out of menu
            if (PhotonNetwork.isMasterClient)
            {
                if (!GameObject.Find("Game Settings"))
                {
                    TimeSlider.value = CurrentTime01;
                }
                else
                {
                    //SyncTimeRPC(currentTime, DayLength,pause);
                }
            }
            //The below syncs the field of view of the moon camera and the main camera, and removes unwanted issues with moon rendering
            //(main camera's field of view changes alot, and if the moon camera's doesnt, it distorts the moon's rendering)
            MoonCamera.fieldOfView = GameObject.Find("MainCamera").GetComponent<Camera>().fieldOfView;
            MoonCamera.transform.rotation = GameObject.Find("MainCamera").transform.rotation;
            if (!pause)
            {
                UpdateLight();
                currentTime += (Time.deltaTime / SecondsInAFullDay) * 24;
                if (CurrentTime01 >= 1)
                {
                    currentTime = 0;//once we hit "midnight"; any time after that sunrise will begin.
                    currentDay++; //make the day counter go up
                }
                if (frames == lightingUpdateInterval) { frames = 0; }
            }
            else
            {
                if (frames == lightingUpdateInterval)
                {
                    frames = 0;
                    UpdateLight();
                }
            }
            frames++;
            //MC loads settings
            if (PhotonNetwork.isMasterClient)
            {
                PhotonView photonView = PhotonView.Get(this);
                
                //photonView.RPC("SyncTimeRPC", PhotonTargets.All, currentTime, DayLength, pause);
                GameSettings.Time.currentTime = currentTime;
                GameSettings.Time.dayLength = SecondsInAFullDay;
                GameSettings.Time.pause = pause;
                Debug.Log("Current Master Client time: " + GameSettings.Time.currentTime);
            }

            //TODO : add a OnJoinedRoom method so that on join, the MC's current time is loaded
            //TODO : resynch time once mc leaves the game settings, because by default while they are
            //in time settings the system is paused(but not for non-mc) , alternatively make it so
            //that the system is not paused while MC is on timesettings menu

        }


        void UpdateLight()
        {
            Quaternion tilt = Quaternion.AngleAxis(sunTilt, Vector3.forward);
            Quaternion rot = Quaternion.AngleAxis((CurrentTime01 * 360) - 90, Vector3.right);

            directionalLight.transform.rotation = tilt * rot; // Yes axial tilt
            directionalLight.transform.Rotate(Vector3.up, sunRotationOffset - 90, Space.World);
            moon.transform.forward = -directionalLight.transform.forward;

            if (timecycle)
            {
                // Sun & moon's color and brightness
                if (timecycle.overrideSunlight)
                {
                    directionalLight.color = timecycle.sunlightColor.Evaluate(CurrentTime01);
                    directionalLight.intensity = timecycle.sunlightColor.Evaluate(CurrentTime01).a * timecycle.maxSunlightIntensity;
                }
                if (timecycle.overrideMoonlight)
                {
                    Light moonLight = moon.GetComponent<Light>();
                    moonLight.color = timecycle.moonlightColor.Evaluate(CurrentTime01);
                    moonLight.intensity = timecycle.moonlightColor.Evaluate(CurrentTime01).a * timecycle.maxMoonlightIntensity;
                }

                // Environment lighting
                if (timecycle.overrideEnvironmentLighting)
                {
                    switch (timecycle.lightingOverrideMode)
                    {
                        case TimecycleProfile.AmbientLightingOverrideMode.Gradient:
                            RenderSettings.ambientSkyColor = timecycle.skyColor.Evaluate(CurrentTime01);
                            RenderSettings.ambientEquatorColor = timecycle.equatorColor.Evaluate(CurrentTime01);
                            RenderSettings.ambientGroundColor = timecycle.groundColor.Evaluate(CurrentTime01);
                            break;
                        case TimecycleProfile.AmbientLightingOverrideMode.Color:
                            RenderSettings.ambientLight = timecycle.lightingColor.Evaluate(CurrentTime01);
                            break;
                    }
                }

                // Fog
                if (timecycle.overrideFog)
                {
                    RenderSettings.fogColor = timecycle.fogColor.Evaluate(CurrentTime01);
                    RenderSettings.fogDensity = timecycle.fogColor.Evaluate(CurrentTime01).a * timecycle.maxFogDensity;
                }
            }

            //change skybox to add mood

            // RenderSettings.skybox.SetColor("_SkyTint",timecycle.skyColor.Evaluate(CurrentTime01));
            // RenderSettings.skybox.SetColor("_GroundColor",timecycle.groundColor.Evaluate(CurrentTime01));
        }

        public string TimeOfDay()
        {
            // Consider using switch-case here. It will probably improve the performance.
            string dayState = "";
            if (CurrentTime01 > 0f  && CurrentTime01 < 0.1f )
            {
                dayState = "Midnight";
            }
            if (CurrentTime01 < 0.5f  && CurrentTime01 > 0.1f )
            {
                dayState = "Morning";
            }
            if (CurrentTime01 > 0.5f  && CurrentTime01 < 0.6f)
            {
                dayState = "Mid Noon";
            }
            if (CurrentTime01 > 0.6f && CurrentTime01 < 0.8f)
            {
                dayState = "Evening";
            }
            if (CurrentTime01 > 0.8f && CurrentTime01 < 1f)
            {
                dayState = "Night";
            }
            return dayState;
        }
        
    }
}
