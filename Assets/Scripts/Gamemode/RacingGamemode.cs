﻿using Assets.Scripts.Gamemode.Racing;
using Assets.Scripts.Settings;
using Assets.Scripts.Settings.Gamemodes;
using Assets.Scripts.UI.InGame.HUD;
using Assets.Scripts.UI.Input;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Gamemode
{
    public class RacingGamemode : GamemodeBase
    {
        public string localRacingResult = string.Empty;
        public List<RacingObjective> Objectives = new List<RacingObjective>();
        public List<RacingStartBarrier> StartBarriers = new List<RacingStartBarrier>();

        private RacingSettings Settings => GameSettings.Gamemode as RacingSettings;
        private const float CountDownTimerLimit = 20f;

        private bool HasStarted { get; set; }

        private float TotalSpeed { get; set; }
        private int TotalFrames { get; set; }
        private float AverageSpeed => TotalSpeed / TotalFrames;

        public override void OnGameWon()
        {
            FengGameManagerMKII.instance.gameEndCD = Settings.RestartOnFinish.Value
                ? 20f
                : 9999f;

            var parameters = new object[] { 0 };
            FengGameManagerMKII.instance.photonView.RPC("netGameWin", PhotonTargets.Others, parameters);
            if (((int) FengGameManagerMKII.settings[0xf4]) == 1)
            {
                //this.chatRoom.addLINE("<color=#FFC000>(" + this.roundTime.ToString("F2") + ")</color> Round ended (game win).");
            }
        }

        private void OnLevelWasLoaded()
        {
            HasStarted = false;

            if (!PhotonNetwork.isMasterClient)
                photonView.RPC(nameof(RequestStatus), PhotonTargets.MasterClient);

            if (Objectives.Count == 0) return;
            Objectives = Objectives.OrderBy(x => x.Order).ToList();
            for (int i = 0; i < Objectives.Count; i++)
            {
                if (i + 1 >= Objectives.Count) continue;
                Objectives[i].NextObjective = Objectives[i + 1];
            }
            Objectives[0].Current();
        }

        private void Update()
        {
            if (HasStarted)
            {
                //TODO Refactor the average speed to be more performance friendly
                var currentSpeed = Camera.main.GetComponent<IN_GAME_MAIN_CAMERA>().main_object?.GetComponent<Rigidbody>().velocity.magnitude ?? 0f;
                TotalSpeed += currentSpeed;
                TotalFrames++;

                UiService.SetMessage(LabelPosition.Top, $"Time: {TimeService.GetRoundTime() - CountDownTimerLimit:F1} | " +
                                                        $"Average Speed: {AverageSpeed:F1}");
            }
            else
            {
                UiService.SetMessage(LabelPosition.Center, $"RACE START IN {CountDownTimerLimit - TimeService.GetRoundTime():F1}s");
                if (CountDownTimerLimit - TimeService.GetRoundTime() <= 0f)
                {
                    HasStarted = true;
                    if (PhotonNetwork.isMasterClient)
                    {
                        photonView.RPC(nameof(RacingStartRpc), PhotonTargets.All);
                    }
                }
            }
        }

        [PunRPC]
        private void RequestStatus(PhotonMessageInfo info)
        {
            if (!PhotonNetwork.isMasterClient) return;

            photonView.RPC(nameof(RacingStartRpc), info.sender);
        }

        [PunRPC]
        private void RacingStartRpc(PhotonMessageInfo info)
        {
            if (!info.sender.IsMasterClient) return;

            StartBarriers.ForEach(x => x.gameObject.SetActive(false));
            UiService.ResetMessage(LabelPosition.Center);
        }


        public override string GetVictoryMessage(float timeUntilRestart, float totalServerTime = 0f)
        {
            if (PhotonNetwork.offlineMode)
            {
                var num = (((int)(totalServerTime * 10f)) * 0.1f) - 5f;
                return $"{num}s !!\n Press {InputManager.GetKey(InputUi.Restart)} to Restart.\n\n\n";
            }
            return $"{localRacingResult}\n\nGame Restart in {(int) timeUntilRestart}";
        }

        public override void OnNetGameWon(int score)
        {
            FengGameManagerMKII.instance.gameEndCD = Settings.RestartOnFinish.Value
                ? 20f
                : 9999f;
        }

        protected override IEnumerator OnUpdateEverySecond()
        {
            yield break;
        }

        protected override IEnumerator OnUpdateEveryTenthSecond()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.1f);
                SetStatusTopLeft();
            }
        }

        protected override void SetStatusTopLeft()
        {
            //    //this.currentSpeed = Camera.main.GetComponent<IN_GAME_MAIN_CAMERA>().main_object
            //    //    .GetComponent<Rigidbody>().velocity.magnitude;
            //    this.maxSpeed = Mathf.Max(this.maxSpeed, this.currentSpeed);
            //    this.ShowHUDInfoTopLeft(string.Concat(new object[]
            //        {"Current Speed : ", (int) this.currentSpeed, "\nMax Speed:", this.maxSpeed}));
        }
    }
}