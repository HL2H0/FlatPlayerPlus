using Il2CppSLZ.Marrow;
using MelonLoader;
using BoneLib;
using UnityEngine;
using Il2CppSLZ.Bonelab.SaveData;
using FlatPlayer;

[assembly: MelonInfo(typeof(FlatPlayerPlus.Mod), "FlatPlayerPlus", "1.0.0", "HL2H0",  null)]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace FlatPlayerPlus    
{
    public class Mod : MelonMod
    {
        
        private static ReloadHand _reloadHand = ReloadHand.Right;       
        
        public enum ReloadHand
        {
            Right,
            Left
        }
        
        public static void ReloadGun(ReloadHand reloadHand)
        {
            switch (reloadHand)
            {
                case ReloadHand.Right:
                    Gun rightGun = Player.GetComponentInHand<Gun>(Player.RightHand);
                    Magazine leftMagazine = Player.GetComponentInHand<Magazine>(Player.LeftHand);

                    if (rightGun && leftMagazine && !rightGun.HasMagazine())
                    {
                        leftMagazine.Despawn();
                        
                        rightGun.InstantLoadAsync();
                        rightGun.CompleteSlidePull();
                        rightGun.CompleteSlideReturn();
                    }
                    break;
                
                case ReloadHand.Left:
                    Gun leftGun = Player.GetComponentInHand<Gun>(Player.LeftHand);
                    Magazine rightMagazine = Player.GetComponentInHand<Magazine>(Player.RightHand);
                    
                    if (rightMagazine != null && !leftGun.HasMagazine())
                    {
                        rightMagazine.Despawn();
                        
                        leftGun.InstantLoadAsync();
                        leftGun.CompleteSlidePull();
                        leftGun.CompleteSlideReturn();
                    }
                    break;
            }
        }
        
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("FlatPlayer+ 1.0.0 Initialized.");
        }
        
        public override void OnUpdate()
        {
            base.OnUpdate();
            _reloadHand = DataManager.ActiveSave.PlayerSettings.BeltLocationRight == false
                ? ReloadHand.Right
                : ReloadHand.Left;

            if (!FlatBooter.IsReady) return;
            
            if (Input.GetKeyDown(KeyCode.R) && FlatBooter.IsReady) 
                ReloadGun(_reloadHand);

            if (Input.GetKey(KeyCode.W) && Player.RigManager.activeSeat)
                Player.RightController._thumbstickAxis = new Vector2(0, 1);

            if (Input.GetKeyDown(KeyCode.LeftControl))
                Player.LeftController._thumbstickDown = true;
        }
    }
}