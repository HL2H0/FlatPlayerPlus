using System.Reflection;
using Il2CppSLZ.Marrow;
using MelonLoader;
using BoneLib;
using BoneLib.BoneMenu;
using UnityEngine;
using Il2CppSLZ.Bonelab.SaveData;
using FlatPlayer;
using FlatPlayerPlus.MonoBehaviours;
using AmmoInventory = Il2CppSLZ.Marrow.AmmoInventory;
using Avatar = Il2CppSLZ.VRMK.Avatar;
using Object = UnityEngine.Object;
using Page = BoneLib.BoneMenu.Page;

[assembly: MelonInfo(typeof(FlatPlayerPlus.Mod), "FlatPlayerPlus", "2.0.0", "HL2H0")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace FlatPlayerPlus    
{
    public class Mod : MelonMod
    {
        private static Page _mainPage;
        private static Page _graphicsPage;
        private static Page _togglesPage;
        private static Page _uiPage;
        private static Page _flatPlayerPage;
        
        private static ReloadHand _reloadHand = ReloadHand.Right;
        
       public static EnumElement FullscreenModeElement;
       public static BoolElement VSyncElement;
       public static FloatElement FOVElement;
       public static IntElement FPSLimit;
       public static BoolElement ToggleRightGripElement;
       public static BoolElement ToggleLeftGripElement;
       public static EnumElement HealthBarPositionElement;
       public static EnumElement AmmoPositionElement;
       public static FloatElement HandsExtendSensitivity;
       public static FloatElement CameraSmoothness;
       
        
        
        private static bool _rightGripToggled;
        private static bool _leftGripToggled;
        
        private static GameObject _uiPrefab;
        
        private static GameObject _ui;
        public static FPP_UI_Handler UIHandler;
        
        private enum ReloadHand
        {
            Right,
            Left
        }
        
      
        private static void ReloadGun(ReloadHand reloadHand)
        {
            switch (reloadHand)
            {
                case ReloadHand.Right:
                    var rightGun = Player.GetComponentInHand<Gun>(Player.RightHand);
                    var leftMagazine = Player.GetComponentInHand<Magazine>(Player.LeftHand);

                    if (rightGun && leftMagazine && !rightGun.HasMagazine())
                    {
                        leftMagazine.Despawn();
                        
                        rightGun.InstantLoadAsync();
                        rightGun.CompleteSlidePull();
                        rightGun.CompleteSlideReturn();
                    }
                    break;
                
                case ReloadHand.Left:
                    var leftGun = Player.GetComponentInHand<Gun>(Player.LeftHand);
                    var rightMagazine = Player.GetComponentInHand<Magazine>(Player.RightHand);
                    
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

        private void SetupBoneMenu()
        {
            
            FPSLimit = new IntElement("FPS Limit", Color.white, 60, 1, 0, int.MaxValue, v =>
            {
                Application.targetFrameRate = v;
                ModPreferences.SavePreferences();
            });

            _mainPage = Page.Root.CreatePage("FlatPlayerPlus", Color.green);
            _togglesPage = _mainPage.CreatePage("Toggles", Color.yellow);
            ToggleRightGripElement = _togglesPage.CreateBool("Toggle Right Grip", Color.white, false, _ =>
            {
                ModPreferences.SavePreferences();
            });
            ToggleLeftGripElement = _togglesPage.CreateBool("Toggle Left Grip", Color.white, false, null);
            
            _uiPage = _mainPage.CreatePage("UI", Color.magenta);
            HealthBarPositionElement = _uiPage.CreateEnum("Health Bar Position", Color.white, FPP_UI_Handler.HealthBarUIPosition,
                v =>
                {
                    UIHandler.UpdateUIPosition((FPP_UI_Handler.UIPosition)v, (FPP_UI_Handler.UIPosition)AmmoPositionElement.Value);
                });
            AmmoPositionElement = _uiPage.CreateEnum("Ammo Position", Color.white, FPP_UI_Handler.AmmoUIPosition, v =>
            {
                UIHandler.UpdateUIPosition((FPP_UI_Handler.UIPosition)HealthBarPositionElement.Value, (FPP_UI_Handler.UIPosition)v);
            });
            
            _flatPlayerPage = _mainPage.CreatePage("FlatPlayer Settings", Color.yellow);
            HandsExtendSensitivity = _flatPlayerPage.CreateFloat("Hand Extend Sensitivity", Color.white, 0.1f, 0.1f, 0, int.MaxValue, _ =>
            {
                ModPreferences.SavePreferences();
                FlatBooter.Instance.ReloadConfig();
            });
            CameraSmoothness = _flatPlayerPage.CreateFloat("Camera Smoothness", Color.white, 0.3f, 0.1f, 0, 1, _ =>
            {
                ModPreferences.SavePreferences();
                FlatBooter.Instance.ReloadConfig();
            });
            _graphicsPage = _mainPage.CreatePage("Graphics", Color.green);
            FOVElement = _graphicsPage.CreateFloat("FOV", Color.white, 90, 1, 0, int.MaxValue, v =>
            {
                FlatBooter.MainCamera.fieldOfView = v;
                ModPreferences.SavePreferences();
            });
            FullscreenModeElement = _graphicsPage.CreateEnum("Full Screen Mode", Color.white, Screen.fullScreenMode,
                v =>
                {
                    Screen.fullScreenMode = (FullScreenMode)v;
                    ModPreferences.SavePreferences();
                });
            VSyncElement = _graphicsPage.CreateBool("V-Sync", Color.white, false, v =>
            {
                QualitySettings.vSyncCount = v ? 1 : 0;
                if (v)
                    _mainPage.Remove(FPSLimit);
                else
                {
                    _mainPage.Add(FPSLimit);
                    Application.targetFrameRate = FPSLimit.Value;
                }
                ModPreferences.SavePreferences();
            });
        }

        private static void InitializeBundles()
        {
            FieldInjector.SerialisationHandler.Inject<FPP_UI_Handler>();
            var bundlePath = "FlatPlayerPlus.Resources.flatplayerplus.pack";
            var bundle = HelperMethods.LoadEmbeddedAssetBundle(Assembly.GetExecutingAssembly(), bundlePath);
            _uiPrefab = HelperMethods.LoadPersistentAsset<GameObject>(bundle, "FP+ UI");
        }
        
        public override void OnInitializeMelon()
        {
            SetupBoneMenu();
            InitializeBundles();
            ModPreferences.CreatePreferences();
            Hooking.OnUIRigCreated += HookingOnOnUIRigCreated;
            Hooking.OnSwitchAvatarPostfix += OnAvatarSwitch;
            LoggerInstance.Msg("FlatPlayer+ 2.0.0 Initialized.");
        }

        private static void OnAvatarSwitch(Avatar obj)
        {
            UIHandler.MaxHealth = Player.RigManager.health.max_Health;
        }
        
        private static void HookingOnOnUIRigCreated()
        {   
            if (_uiPrefab != null)
            { 
                _ui = Object.Instantiate(_uiPrefab);
                UIHandler = _ui.GetComponent<FPP_UI_Handler>();
                UIHandler.MaxHealth = Player.RigManager.health.max_Health;
                ModPreferences.LoadPreferences();
            }
        }
        
        public override void OnUpdate()
        {
            base.OnLateUpdate();
            if (!FlatBooter.IsReady) return;

            UIHandler.CurrHealth = Player.RigManager.health.curr_Health;
            
            var lightAmmo = AmmoInventory.Instance._groupCounts["light"];
            var mediumAmmo = AmmoInventory.Instance._groupCounts["medium"];
            var heavyAmmo = AmmoInventory.Instance._groupCounts["heavy"];
            UIHandler.UpdateAmmoText(lightAmmo, mediumAmmo, heavyAmmo);
            
            //Get belt location
            _reloadHand = DataManager.ActiveSave.PlayerSettings.BeltLocationRight == false
                ? ReloadHand.Right
                : ReloadHand.Left;
            
            //Reload
            if (Input.GetKeyDown(ModPreferences.ReloadKey.Value)) 
                ReloadGun(_reloadHand);
            
            //DriveForwards
            if (Input.GetKey(ModPreferences.DriveForwardKey.Value) && Player.RigManager.activeSeat)
            {
                Player.RightController._thumbstickAxis = new Vector2(0, 1);
                Player.LeftController._thumbstickAxis = new Vector2(0, -1);
            }
            
            //Ragdoll
            if (Input.GetKeyDown(ModPreferences.RagdollKey.Value))
                Player.LeftController._thumbstickDown = true;
        }

        public override void OnLateUpdate()
        {
            base.OnLateUpdate();
            if (!FlatBooter.IsReady) return;
            
            if (Input.GetMouseButtonDown(0))
                _leftGripToggled = !_leftGripToggled;
            
            if (Input.GetMouseButtonDown(1))
                _rightGripToggled = !_rightGripToggled;
            
            var rightGrip = _rightGripToggled ? 1 : 0;
            var leftGrip = _leftGripToggled ? 1 : 0;
            
            if(ToggleRightGripElement.Value)
                FlatBooter.RightController.Grip = rightGrip;
            if(ToggleLeftGripElement.Value)
                FlatBooter.LeftController.Grip = leftGrip;
        }
    }
}