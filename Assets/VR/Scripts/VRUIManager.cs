using DaggerfallWorkshop.Game;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DaggerfallWorkshop.Game.UserInterface;
using UnityEngine.EventSystems;

/** * This component disables the default UI and replaces it with VR logic
 **/
public class VRUIManager : MonoBehaviour
{
    public const string k_uiLayerName = "UI";
    public const string k_playerLayerName = "Player";
    public const string k_vrEquipmentLayerName = "VREquipment";

    public GameObject FloatingUIPrefab;
    public GameObject FollowingUIPrefab;
    
    private Camera mainCamera { get { return GameManager.Instance.MainCamera; } }
    private GameObject followingUI;
    private GameObject floatingUI;

    public bool IsOpen { get { return floatingUI && floatingUI.activeSelf; } }
    private IUserInterfaceManager uiManager;

    // Used for enabling/disabling the floating UI
    private int cachedMask = 0;
    // used to hide game world when floating UI is up
    private int playerLayerMask;
    private int uiLayerMask;
    private int vrEquipmentLayerMask;

    #region Singleton

    public static VRUIManager Instance { get; private set; }

    private void SetupSingleton()
    {
        if (!Instance)
            Instance = this;
        else
        {
            Debug.LogError("2nd instance of VRUIManager singleton spawned. There should only be one.");
        }
    }

    #endregion

    private void Awake()
    {
        SetupSingleton();
        playerLayerMask = LayerMask.GetMask(k_playerLayerName);
        uiLayerMask = LayerMask.GetMask(k_uiLayerName);
        vrEquipmentLayerMask = LayerMask.GetMask(k_vrEquipmentLayerName);

        DaggerfallWorkshop.Game.Serialization.SaveLoadManager.OnStartLoad += SaveLoadManager_OnStartLoad;
    }

    private void SaveLoadManager_OnAttemptLoad(string path)
    {
        DisableUI();
    }

    void Start()
    {
        if (FloatingUIPrefab) {
            floatingUI = Instantiate(FloatingUIPrefab);
        } else {
            Debug.LogError("The VR UI Manager was unable to create the floating UI! The VR UI will be very broken.");
            return;
        }

        if (FollowingUIPrefab) {
            followingUI = Instantiate(FollowingUIPrefab);
        } else {
            Debug.LogError("The VR UI Manager was unable to create the Following UI! The VR UI will be somewhat broken.");
            return;
        }
        
        mainCamera.backgroundColor = new Color(.1f, .1f, .1f);
        cachedMask = mainCamera.cullingMask;
        StickFloatingUIInFrontOfPlayer();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        floatingUI.SetActive(false);

        followingUI.GetComponent<FollowingUI>().whatToFollow = mainCamera.gameObject;

        uiManager = DaggerfallUI.UIManager;
        uiManager.OnWindowChange += UIManager_OnWindowChange;

        cachedMask = mainCamera.cullingMask;

        //add our InputModule
        FindObjectOfType<EventSystem>().gameObject.AddComponent<ViveControllerInput>();
    }
    private void OnDestroy()
    {
        if(uiManager != null)
            uiManager.OnWindowChange -= UIManager_OnWindowChange;
        DaggerfallWorkshop.Game.Serialization.SaveLoadManager.OnStartLoad -= SaveLoadManager_OnStartLoad;
    }

    public void CloseAllDaggerfallWindows()
    {
        while (DaggerfallUI.UIManager.WindowCount > 0)
            DaggerfallUI.UIManager.PopWindow();
    }
    private void UIManager_OnWindowChange(object sender, System.EventArgs e)
    {
        int windowCount = uiManager.WindowCount;
        if (windowCount > 0 && !floatingUI.activeSelf)
        {
            // Window count increased--display the UI!
            EnableUI();
        }
        else if (windowCount <= 0)
        {
            // Window count decreased--disable the UI
            DisableUI();
        }
    }
    public void EnableUI()
    {
        if (floatingUI.activeSelf) //already enabled
            return;
        floatingUI.SetActive(true);
        mainCamera.cullingMask = uiLayerMask | playerLayerMask | vrEquipmentLayerMask;
        StickFloatingUIInFrontOfPlayer();
    }
    public void DisableUI()
    {
        if (!floatingUI.activeSelf) //already disabled
            return;
        floatingUI.SetActive(false);
        mainCamera.cullingMask = cachedMask;


    }
    void StickFloatingUIInFrontOfPlayer() {
        if (!floatingUI || !mainCamera) return;
        //set position
        Vector3 floatPos = mainCamera.transform.position + (mainCamera.transform.forward * 3f);
        floatPos.y = mainCamera.transform.position.y;
        floatingUI.transform.position = floatPos;
        //set rotation
        Vector3 lookPos = mainCamera.transform.position;
        lookPos.y = floatingUI.transform.position.y;
        floatingUI.transform.LookAt(lookPos);
        floatingUI.transform.Rotate(Vector3.up, 180);
    }

    private void SaveLoadManager_OnStartLoad(DaggerfallWorkshop.Game.Serialization.SaveData_v1 saveData)
    {
        DisableUI();
    }

}