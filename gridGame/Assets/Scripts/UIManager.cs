using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public class UnitMenuOption : MonoBehaviour
    {
        public UnitMenuButtons option = UnitMenuButtons.WAIT;
        public int cost = 0;

        public UnitMenuOption(UnitMenuButtons opt, int inCost)
        {
            option = opt;
            cost = inCost;
        }
    }

    public enum UnitMenuButtons
    {
        WAIT,
        ATTACK,
        SUNDER,
        FORTIFY,
        HEAL,
        REFRESH
    }

    public Image cursor;
    public float cursorSmoothing;
    public bool freezeCursor;

    [Header("UI GameObjects")]
    public GameObject floatingText;
    public GameObject explosionEffect;
    public static GameObject explosionEffectStatic;
    public bool displayingInfoPanels;

    [Header("Turn Phase Screen")]
    public Image turnPhasePanel;
    public TMP_Text currentTeamUI;
    public TMP_Text currentTurnUI;
    private bool faded;
    public float fadeDuration;
    public float holdDuration;

    [Header("Tile Panel")]
    public Image tilePanelBackground;
    public Image tileSprite;
    public TMP_Text tileName;
    public TMP_Text tileDefense;
    public TMP_Text tileHP;

    [Header("Unit Panel")]
    public Image unitPanelBackground;
    public Image unitSprite;
    public Image unitHPBar;

    public TMP_Text unitName;
    public TMP_Text unitClass;

    public TMP_Text unitStrength;
    public TMP_Text unitCritChance;
    public TMP_Text unitCritAvoid;

    public TMP_Text unitHealth;

    [Header("Unit Menu")]
    public Image unitMenu;
    public Button waitButton;
    public TMP_Text waitCost;

    public Button attackButton;
    public TMP_Text atkCost;

    public Button sunderButton;
    public TMP_Text sunderCost;

    public Button fortifyButton;
    public TMP_Text fortifyCost;

    public Button healButton;
    public TMP_Text healCost;

    public Button refreshButton;
    public TMP_Text refreshCost;


    [Header("Game Menu")]
    public Image gameMenu;
    //public Button endTurnButton;

    [Header("Action Point")]
    public TMP_Text currentActionPoints;

    [Header("Other Stuff")]
    public GameManager gameMan;

    private Vector2 desiredCursorPosition = new Vector2();

    private void Start()
    {
        explosionEffectStatic = explosionEffect;

        HideMenu(unitMenu);
        HideMenu(gameMenu);
        ShowMenu(turnPhasePanel);
        turnPhasePanel.GetComponent<CanvasGroup>().alpha = 0.0f;

        faded = true;
        displayingInfoPanels = true;

        BlinkTurnPhasePanel();
    }

    void Update()
    {
        UpdateCursorPosition();
        UpdateUIPanels();
        UpdateAPCounter();
    }

    public void UpdateUIPanels()
    {
        updateCurrentTilePanel();
        updateCurrentUnitPanel();
    }

    private void UpdateCursorPosition()
    {
        freezeCursor = gameMan.showingAttackRange ||
            gameMan.unitInMovement ||
            AreAnyMenusVisible() ||
            turnPhasePanel.GetComponent<CanvasGroup>().alpha > 0.08f;

        //if (!unitMenu.gameObject.activeInHierarchy)
        if (!freezeCursor)
        {
            cursor.transform.position = Vector2.Lerp(cursor.transform.position, desiredCursorPosition, Time.deltaTime * cursorSmoothing);
        }
    }

    public void SetDesiredCursorPosition(Vector2 inV)
    {
        desiredCursorPosition.x = inV.x;
        desiredCursorPosition.y = inV.y;
    }

    private void updateCurrentTilePanel()
    {
        if (!displayingInfoPanels)
        {
            tilePanelBackground.gameObject.SetActive(false);
        }
        else
        {
            tilePanelBackground.gameObject.SetActive(true);
        }

        if (gameMan.tileBeingDisplayed != null)
        {
            Tile t = gameMan.displayedTileInfo;

            tileName.text = t.name;
            tileDefense.text = "Def. " + t.defenseModifier;
            if (gameMan.tileBeingDisplayed.GetComponent<ClickableTileScript>().belongsToTeam == null)
            {
                tileHP.text = "";
            }
            else
            {
                tileHP.text = "HP " + t.tileHP;
            }
            tileSprite.sprite = t.mapSprite;
        }
    }

    private void updateCurrentUnitPanel()
    {
        if(gameMan.unitBeingDisplayed == null || !displayingInfoPanels)
        {
            unitPanelBackground.gameObject.SetActive(false);
        }
        else if(displayingInfoPanels)
        {
            unitPanelBackground.gameObject.SetActive(true);

            Unit u = gameMan.unitBeingDisplayed.GetComponent<Unit>();

            unitName.text = u.unitName;
            unitClass.text = u.unitClass.ToString();

            unitStrength.text = "Attack: " + u.attackDamage;
            unitCritChance.text = "Crit chance: " + u.chanceToCrit + "%";
            unitCritAvoid.text = "Crit avoid: " + u.critAvoid + "%";

            //HP + HP bar
            unitHealth.text = u.currentHealthPoints + " / " + u.maxHealthPoints;
            float hpVal = (float)u.currentHealthPoints / (float)u.maxHealthPoints;
            Vector3 scaleV = new Vector3(hpVal, 1, 1);
            unitHPBar.transform.localScale = scaleV;
            unitHPBar.color = u.GetTeamColor();

            unitSprite.sprite = u.unitSprite;
        }
    }

    private void UpdateAPCounter()
    {
        if (gameMan.usedAP <= 0)
        {
            currentActionPoints.color = Color.white;
            currentActionPoints.text = FormatInteger(gameMan.currentAP);
        }
        else
        {
            currentActionPoints.color = Color.red;
            currentActionPoints.text = FormatInteger(gameMan.currentAP - gameMan.usedAP);
        }
    }

    public void AdjustUnitMenu(List<UnitMenuOption> enabledBtns, List<UnitMenuOption> disabledBtns)
    {
        int numBtns = enabledBtns.Count + disabledBtns.Count;
        float buttonHeight = waitButton.GetComponent<RectTransform>().rect.height;
        float spaceBetweenButtons = 0.1f;
        float newHeight = spaceBetweenButtons + (numBtns * (spaceBetweenButtons + buttonHeight));
        float anchorDelta = (float)(buttonHeight + spaceBetweenButtons) / (float)newHeight;

        unitMenu.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);

        AllUnitMenuButtonsActive(false);

        int i = 1;
        foreach (UnitMenuOption btn in disabledBtns)
        {
            GreyOutUnitMenuButton(btn.option);

            RectTransform rt = GetUnitMenuButton(btn.option).GetComponent<RectTransform>();
            GetUnitMenuButtonCost(btn.option).text = btn.cost.ToString();
            Vector2 newAnchor = new Vector2(0.5f, i * anchorDelta);
            rt.anchorMin = newAnchor;
            rt.anchorMax = newAnchor;

            i++;
        }
        foreach (UnitMenuOption btn in enabledBtns)
        {
            DisableUnitMenuButton(btn.option, false);

            RectTransform rt = GetUnitMenuButton(btn.option).GetComponent<RectTransform>();
            GetUnitMenuButtonCost(btn.option).text = btn.cost.ToString();
            Vector2 newAnchor = new Vector2(0.5f, i * anchorDelta);
            rt.anchorMin = newAnchor;
            rt.anchorMax = newAnchor;

            i++;
        }
    }

    public void DisableUnitMenuButton(UnitMenuButtons toDisable, bool option)
    {
        switch (toDisable)
        {
            case UnitMenuButtons.WAIT:
                waitButton.interactable = !option;
                waitButton.gameObject.SetActive(!option);
                break;
            case UnitMenuButtons.ATTACK:
                attackButton.interactable = !option;
                attackButton.gameObject.SetActive(!option);
                break;
            case UnitMenuButtons.SUNDER:
                sunderButton.interactable = !option;
                sunderButton.gameObject.SetActive(!option);
                break;
            case UnitMenuButtons.FORTIFY:
                fortifyButton.interactable = !option;
                fortifyButton.gameObject.SetActive(!option);
                break;
            case UnitMenuButtons.HEAL:
                healButton.interactable = !option;
                healButton.gameObject.SetActive(!option);
                break;
            case UnitMenuButtons.REFRESH:
                refreshButton.interactable = !option;
                refreshButton.gameObject.SetActive(!option);
                break;
        }
    }

    public void GreyOutUnitMenuButton(UnitMenuButtons toDisable)
    {
        switch (toDisable)
        {
            case UnitMenuButtons.WAIT:
                waitButton.interactable = false;
                waitButton.gameObject.SetActive(true);
                break;
            case UnitMenuButtons.ATTACK:
                attackButton.interactable = false;
                attackButton.gameObject.SetActive(true);
                break;
            case UnitMenuButtons.SUNDER:
                sunderButton.interactable = false;
                sunderButton.gameObject.SetActive(true);
                break;
            case UnitMenuButtons.FORTIFY:
                fortifyButton.interactable = false;
                fortifyButton.gameObject.SetActive(true);
                break;
            case UnitMenuButtons.HEAL:
                healButton.interactable = false;
                healButton.gameObject.SetActive(true);
                break;
            case UnitMenuButtons.REFRESH:
                refreshButton.interactable = false;
                refreshButton.gameObject.SetActive(true);
                break;
        }
    }

    private Button GetUnitMenuButton(UnitMenuButtons toFind)
    {
        switch (toFind)
        {
            case UnitMenuButtons.WAIT:
                return waitButton;
            case UnitMenuButtons.ATTACK:
                return attackButton;
            case UnitMenuButtons.SUNDER:
                return sunderButton;
            case UnitMenuButtons.FORTIFY:
                return fortifyButton;
            case UnitMenuButtons.HEAL:
                return healButton;
            case UnitMenuButtons.REFRESH:
                return refreshButton;
            default:
                Debug.Log("Button not yet implemented!");

                return waitButton;
        }
    }

    private TMP_Text GetUnitMenuButtonCost(UnitMenuButtons toFind)
    {
        switch (toFind)
        {
            case UnitMenuButtons.WAIT:
                return waitCost;
            case UnitMenuButtons.ATTACK:
                return atkCost;
            case UnitMenuButtons.SUNDER:
                return sunderCost;
            case UnitMenuButtons.FORTIFY:
                return fortifyCost;
            case UnitMenuButtons.HEAL:
                return healCost;
            case UnitMenuButtons.REFRESH:
                return refreshCost;
            default:
                Debug.Log("No cost for this button!");

                return waitCost;
        }
    }

    public void AllUnitMenuButtonsActive(bool toSet)
    {
        foreach(Transform button in unitMenu.transform)
        {
            button.GetComponent<Button>().interactable = toSet;
            button.GetComponent<Button>().gameObject.SetActive(toSet);
        }
    }

    public void ShowMenu(Image menuToShow)
    {
        menuToShow.gameObject.SetActive(true);
    }
    public void HideMenu(Image menuToHide)
    {
        menuToHide.gameObject.SetActive(false);
    }
    public bool IsMenuHidden(Image menuToCheck)
    {
        return !menuToCheck.gameObject.activeInHierarchy;
    }
    public bool AreAnyMenusVisible()
    {
        return !IsMenuHidden(unitMenu) || 
            !IsMenuHidden(gameMenu);
    }
    public void SetCurrentTurnText(int turnNum)
    {
        currentTurnUI.SetText("TURN " + turnNum);
    }
    public void SetCurrentTeamUI(int teamNumber)
    {
        currentTeamUI.SetText("Player " + (teamNumber + 1).ToString());
    }

    public void FadeTurnPhasePanel()
    {
        CanvasGroup canvasGroup = turnPhasePanel.GetComponent<CanvasGroup>();

        StartCoroutine(DoFade(canvasGroup, canvasGroup.alpha, faded ? 1 : 0));

        faded = !faded;
    }

    private IEnumerator DoFade(CanvasGroup canvasGroup, float startA, float endA)
    {
        float counter = 0f;

        while(counter < fadeDuration)
        {
            counter += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startA, endA, counter / fadeDuration);

            yield return null;
        }
    }

    public void BlinkTurnPhasePanel()
    {
        CanvasGroup canvasGroup = turnPhasePanel.GetComponent<CanvasGroup>();

        StartCoroutine(DoBlink(canvasGroup, canvasGroup.alpha, faded ? 0.9f : 0));
    }

    private IEnumerator DoBlink(CanvasGroup canvasGroup, float startA, float endA)
    {
        displayingInfoPanels = false;

        float counter = 0f;
        while (counter < fadeDuration)
        {
            //print(counter);
            counter += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startA, endA, counter / fadeDuration);

            yield return null;
        }
        counter = 0f;
        while(canvasGroup.alpha == endA && counter < holdDuration)
        {
            counter += Time.deltaTime;

            yield return null;
        }
        counter = 0f;
        while (counter < fadeDuration)
        {
            counter += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(endA, startA, counter / fadeDuration);

            yield return null;
        }

        displayingInfoPanels = true;
    }

    //Utility function, turns 5 into 05, 2 into 02, etc.
    private string FormatInteger(int n)
    {
        if (n < 10)
        {
            return "0" + n.ToString();
        }
        else
        {
            return n.ToString();
        }
    }
}
