using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using UnityEditorInternal;
using System.Runtime.CompilerServices;

public class MainGame : MonoBehaviour, IDataPersistence
{
    public MainMenu menu;
    public ItemManager itemManager;
    public SoldierManager soldierManager;
    public POIManager poiManager;
    public WeatherGen weather;
    public DipelecGen dipelec;
    public SoundManager soundManager;
    public SetBattlefieldParameters setBattlefieldParameters;

    public bool gameOver, modaTurn, frozenTurn;
    public int maxX, maxY, maxZ;
    public int currentRound, maxRounds, currentTeam, maxTeams, maxTurnTime, tempTeam;
    public Camera cam;
    public Light sun;
    public GameObject battlefield, bottomPlane, outlineArea, notEnoughAPUI, notEnoughMPUI, moveToSameSpotUI;
    public TMP_InputField xPos, yPos, zPos, fallInput, overwatchXPos, overwatchYPos, overwatchRadius, overwatchAngle;
    public TMP_Dropdown moveTypeDropdown, terrainDropdown, shotTypeDropdown, aimTypeDropdown, coverLevelDropdown, targetDropdown, meleeTypeDropdown, attackerWeaponDropdown, meleeTargetDropdown, defenderWeaponDropdown, damageEventTypeDropdown;
    public TextMeshProUGUI moveAP;
    public Toggle coverToggle, meleeToggle;
    Vector3 boundCrossOne = Vector3.zero, boundCrossTwo = Vector3.zero;
    public List<Tuple<string, string>> shotParameters = new(), meleeParameters = new();
    public Tuple<Vector3, string, int, int> tempMove;
    public int tempAP, tempMP;
    public Tuple<Soldier, IAmShootable> tempShooterTarget;

    public UnitDisplayPanel displayPanel;
    public Transform allItemsContentUI, inventoryItemsContentUI, groundItemsContentUI, activeItemPanel, allyButtonContentUI;
    public Soldier activeSoldier;

    //helper functions - game
    public List<Soldier> AllSoldiers()
    {
        return soldierManager.allSoldiers;
    }
    public List<IAmDetectable> AllDetectable()
    {
        return FindObjectsOfType<MonoBehaviour>().OfType<IAmDetectable>().ToList();
    }
    public List<IAmShootable> AllShootable()
    {
        return FindObjectsOfType<MonoBehaviour>().OfType<IAmShootable>().ToList();
    }
    public List<GoodyBox> AllGoodyBoxes()
    {
        return FindObjectsOfType<GoodyBox>().ToList();
    }
    public int RandomNumber(int min, int max)
    {
        return UnityEngine.Random.Range(min, max + 1);
    }
    public int DiceRoll()
    {
        return RandomNumber(1, 6);
    }
    public bool CoinFlip()
    {
        if (RandomNumber(0, 1) == 1)
            return true;

        return false;
    }
    public int Factorial(int number)
    {
        if (number == 0)
            return 1;
        else
        {
            int result = 1;

            while (number != 1)
            {
                result *= number;
                number--;
            }
            return result;
        }
    }
    public float BinomialProbability(int n, int x, float p, float q)
    {
        if (n >= x)
            return Factorial(n) / (Factorial(n - x) * Factorial(x)) * Mathf.Pow(p, x) * Mathf.Pow(q, n - x);
        else
            return 0;
    }
    public float CumulativeBinomialProbability(int n, int x, float p, float q)
    {
        if (n >= x)
        {
            float result = 0;

            while (x <= n)
            {
                result += BinomialProbability(n, x, p, q);
                x++;
            }
            return result;
        }
        else
            return 0;
    }
    public string RandomShotScatterDistance()
    {
        return decimal.Round((decimal)UnityEngine.Random.Range(0.5f, 5.0f), 1).ToString();
    }
    public string RandomShotScatterHorizontal()
    {
        if (CoinFlip())
            return "left";
        else
            return "right";
    }
    public string RandomShotScatterVertical()
    {
        if (CoinFlip())
            return "up";
        else
            return "down";
    }
    public IAmShootable FindShootableById(string id)
    {
        foreach (IAmShootable shootable in AllShootable())
            if (shootable.Id == id)
                return shootable;
        return null;
    }
    public IHaveInventory FindHasInventoryById(string id)
    {
        foreach (IHaveInventory hasInventory in FindObjectsOfType<MonoBehaviour>().OfType<IHaveInventory>())
            if (hasInventory.Id == id)
                return hasInventory;
        return null;
    }
    public void ToggleMute()
    {
        soundManager.noisePlayer.mute = !soundManager.noisePlayer.mute;
    }
    







    //turn resolutions
    public void GameOver(string result)
    {
        gameOver = true;
        print("GameOver");

        if (menu.overrideView)
            menu.ToggleOverrideView();

        menu.FreezeTime();
        menu.DisplaySoldiersGameOver();
        menu.roundIndicator.text = "Game Over";
        menu.teamTurnIndicator.text = result;
        soundManager.PlayGameOverMusic();
    }
    public void SwitchTeam(int team)
    {
        tempTeam = currentTeam;
        currentTeam = team;
    }
    public void StartModaTurn(Soldier modaSoldier)
    {
        //change game parameters
        modaTurn = true;
        SwitchTeam(modaSoldier.soldierTeam);

        //activate the surviving soldier
        modaSoldier.soldierUI.GetComponent<SoldierUI>().OpenSoldierMenu("moda");
        modaSoldier.GenerateAP();
        modaSoldier.modaProtect = false;
    }
    public void EndModaTurn()
    {
        activeSoldier.Kill(null, new() { "Modafinil" });
        modaTurn = false;
        SwitchTeam(tempTeam);
    }
    public void StartFrozenTurn(Soldier frozenSoldier)
    {
        tempAP = frozenSoldier.ap;
        tempMP = frozenSoldier.mp;

        //change game parameters
        frozenTurn = true;
        SwitchTeam(frozenSoldier.soldierTeam);

        //activate the surviving soldier
        frozenSoldier.ap = 100;
        frozenSoldier.soldierUI.GetComponent<SoldierUI>().OpenSoldierMenu("frozen");
        menu.OpenShotUI();
    }
    public void EndFrozenTurn()
    {
        activeSoldier.ap = tempAP;
        activeSoldier.mp = tempMP;
        frozenTurn = false;
        SwitchTeam(tempTeam);
    }
    public void EndTurnNonCoroutine()
    {
        StartCoroutine(EndTurn());
    }
    public IEnumerator EndTurn()
    {
        if (!gameOver)
        {
            if (currentRound <= maxRounds)
            {
                menu.SetTeamTurnOverFlagTo(false);

                EndTeamTurn();
                
                yield return new WaitUntil(() => menu.teamTurnOverFlag == true);

                if (currentTeam < maxTeams)
                {
                    currentTeam += 1;
                }
                else
                {
                    if (currentRound == maxRounds)
                        GameOver("No Winner");
                    else
                    {
                        currentTeam = 1;
                        EndRound();
                        currentRound += 1;
                    }
                }

                //run los lost for intra-turn changes
                if (menu.lostLosUI.transform.Find("OptionPanel").Find("Scroll").Find("View").Find("Content").childCount > 0)
                    StartCoroutine(menu.OpenLostLOSList());

                menu.SetTeamTurnStartFlagTo(false);

                yield return new WaitUntil(() => menu.teamTurnStartFlag == true);

                menu.turnTime = 0;
                soundManager.banzaiPlayed = false;
                StartTurn();
            }
        }
    }
    public void EndTeamTurn()
    {
        //pre xp check stuff
        foreach (Soldier s in AllSoldiers())
        {
            if (s.IsOnturnAndAlive()) //run things that trigger at the end of players turn
            {
                //reset usedap flag
                s.usedAP = false;
                s.usedMP = false;

                //reset bloodletter used on turn flag
                s.bloodLettedThisTurn = false;

                //reset inspired
                s.UnsetInspired();

                //remove donated planner movement
                s.plannerDonatedMove = 0;

                //remove dissuaded
                s.UnsetDissuaded();

                //reset patriot
                s.UnsetPatriotic();

                //increase rounds fielded
                s.roundsFielded++;
                if (s.IsConscious())
                {
                    //only give xp for every 4 rounds coscious on the field
                    s.roundsFieldedConscious++;
                    if (s.roundsFieldedConscious % 4 == 0)
                        menu.AddXpAlert(s, 1, $"Survived {s.roundsFieldedConscious} rounds.", true);
                }

                //increase rounds without food
                s.IncreaseRoundsWithoutFood();

                //decrement stunnage
                if (s.stunnedRoundsVulnerable > 0)
                    s.stunnedRoundsVulnerable--;

                //unset suppression
                s.UnsetSuppression();
            }
            else //run things that trigger at the end of another team's turn
            {
                //unset overwatch
                if (s.IsOnOverwatch())
                    s.UnsetOverwatch();

                //decrement loud action counter
                if (s.loudActionRoundsVulnerable > 0)
                    s.loudActionRoundsVulnerable--;
            }
        }

        menu.CheckXP();

        //post xp stuff
        foreach (Soldier s in AllSoldiers())
        {
            if (s.IsOnturnAndAlive()) //run things that trigger at the end of players turn
            {
                //dish out poison damage
                if (s.IsPoisoned())
                    StartCoroutine(s.TakePoisonDamage());

            }
            else //run things that trigger at the end of another team's turn
            {

            }
        }
    }
    public void EndRound()
    {
        //run things that only happen at the end of a round
    }
    public void StartTurn()
    {
        if (currentRound <= maxRounds)
        {
            FileUtility.WriteToReport($"\nRound {currentRound} | Team: {currentTeam}");
            if (currentTeam == 1)
                StartRound();

            IncreaseRoundsActiveAllClouds();
            StartCoroutine(StartTeamTurn());
        }
    }
    public IEnumerator StartTeamTurn()
    {
        if (menu.damageUI.transform.Find("OptionPanel").Find("Scroll").Find("View").Find("Content").childCount > 0)
            StartCoroutine(menu.OpenDamageList());

        foreach (Soldier s in AllSoldiers())
        {
            if (s.IsOnturnAndAlive())
            {
                //patriot ability
                s.SetPatriotic();

                //run things that trigger at the start of players turn
                if (s.IsInspirer())
                    CheckInspirer(s);

                yield return new WaitUntil(() => menu.inspirerResolvedFlag == true);

                s.GenerateAP();
            }
            else
            {
                //run things that trigger at the start of another team's turn
            }
        }
        menu.CheckXP();
        StartCoroutine(DetectionAlertAll("statChange", false));
        DataPersistenceManager.Instance.SaveGame();
    }
    public void StartRound()
    {

    }

    public string CheckWeatherChange(string lastTurnVis, string currentVis)
    {
        int lastTurnVisVal, currentVisVal;

        lastTurnVisVal = lastTurnVis switch
        {
            "Full" => 0,
            "Good" => 1,
            "Moderate" => 2,
            "Poor" => 3,
            "Zero" => 4,
            _ => 0,
        };
        currentVisVal = currentVis switch
        {
            "Full" => 0,
            "Good" => 1,
            "Moderate" => 2,
            "Poor" => 3,
            "Zero" => 4,
            _ => 0,
        };
        if (currentVisVal < lastTurnVisVal)
            return "increase";
        else if (currentVisVal > lastTurnVisVal)
            return "decrease";
        else
            return "false";
    }

    //draining soldier AP and MP after use during turn
    public void EndSoldierTurn()
    {
        DrainAP();
        DrainMP();
    }

    //cover functions
    public void ConfirmCover()
    {
        if (CheckAP(1))
        {
            DeductAP(1);
            activeSoldier.SetCover();
        }

        menu.CloseTakeCoverUI();
    }

    //playdead functions
    public void CheckPlaydead()
    {
        if (activeSoldier.IsPlayingDead())
            activeSoldier.UnsetPlaydead();
        else
            menu.OpenPlaydeadAlertUI();
    }
    public void ConfirmPlaydead()
    {
        activeSoldier.SetPlaydead();
        menu.ClosePlaydeadAlertUI();
    }

    //overwatch functions
    public void ConfirmOverwatch()
    {
        if (int.TryParse(overwatchXPos.text, out int x) && int.TryParse(overwatchYPos.text, out int y) && int.TryParse(overwatchRadius.text, out int r) && int.TryParse(overwatchAngle.text, out int a))
        {
            if (CheckAP(2))
            {
                DrainAP();
                activeSoldier.SetOverwatch(x, y, r, a);
                menu.CloseOverwatchUI();
            }
        }
    }


    //move functions
    public void FleeSoldier()
    {
        activeSoldier.InstantKill(activeSoldier, new List<string>() { "Flee" });
    }
    public int CalculateFallDamage(Soldier soldier, int fallDistance)
    {
        return Mathf.CeilToInt(Mathf.Pow(fallDistance / 4.0f, 2) / 2.0f - soldier.stats.R.Val);
    }
    public void UpdateMoveAP()
    {
        int ap = 0;
        string move = moveTypeDropdown.options[moveTypeDropdown.value].text;

        if (move.Contains("Full"))
            ap = 3;
        else if (move.Contains("Half"))
            ap = 2;
        else if (move.Contains("Tile") || move.Contains("Exo"))
            ap = 1;

        if (ap > 0)
        {
            if (ap > 1 && activeSoldier.InstantSpeed <= 6)
                ap--;

            if (ap > 1 && activeSoldier.IsSprinter())
                ap--;

            if (coverToggle.isOn)
                ap++;
        }

        menu.moveUI.transform.Find("APCost").Find("APCostDisplay").GetComponent<TextMeshProUGUI>().text = ap.ToString();
    }
    public void UpdateMoveDonated()
    {
        if (moveTypeDropdown.options[moveTypeDropdown.value].text.Contains("Planner"))
            menu.moveUI.transform.Find("MoveDonated").Find("MoveDonatedDisplay").GetComponent<TextMeshProUGUI>().text = activeSoldier.HalfMove.ToString();
    }
    public void UpdateMoveUI()
    {
        if (!menu.clearMoveFlag)
        {
            UpdateMoveAP();
            UpdateMoveDonated();
        }
    }
    public bool GetMoveLocation(out Tuple<Vector3, string> moveLocation)
    {
        moveLocation = default;
        if (xPos.textComponent.GetComponent<TextMeshProUGUI>().color == menu.normalTextColour && yPos.textComponent.GetComponent<TextMeshProUGUI>().color == menu.normalTextColour && zPos.textComponent.GetComponent<TextMeshProUGUI>().color == menu.normalTextColour && terrainDropdown.value != 0)
        {
            moveLocation = Tuple.Create(new Vector3(int.Parse(xPos.text), int.Parse(yPos.text), int.Parse(zPos.text)), terrainDropdown.options[terrainDropdown.value].text);
            return true;
        }

        return false;
    }
    public void ConfirmMove(bool force)
    {
        int.TryParse(moveAP.text, out int ap);

        if (moveTypeDropdown.options[moveTypeDropdown.value].text.Contains("Planner"))
        {
            if (CheckMP(1) && CheckAP(ap))
            {
                //planner donation proceeds
                DeductAP(ap);
                DrainMP();
                foreach (Transform child in menu.moveUI.transform.Find("ClosestAlly").Find("ClosestAllyPanel"))
                    soldierManager.FindSoldierByName(child.Find("SoldierName").GetComponent<TextMeshProUGUI>().text).plannerDonatedMove += activeSoldier.HalfMove;
            }
            menu.CloseMoveUI();
        }
        else if (moveTypeDropdown.options[moveTypeDropdown.value].text.Contains("Exo"))
        {
            if (GetMoveLocation(out Tuple<Vector3, string> moveToLocation))
            {
                if (activeSoldier.X != moveToLocation.Item1.x || activeSoldier.Y != moveToLocation.Item1.y || activeSoldier.Z != moveToLocation.Item1.z)
                {
                    if (force || (moveToLocation.Item1.x <= activeSoldier.X + 3 && moveToLocation.Item1.x >= activeSoldier.X - 3 && moveToLocation.Item1.y <= activeSoldier.Y + 3 && moveToLocation.Item1.y >= activeSoldier.Y - 3))
                    {
                        if (CheckAP(ap))
                        {
                            PerformMove(activeSoldier, ap, moveToLocation, meleeToggle.isOn, coverToggle.isOn, fallInput.text, true);
                            //trigger loud action
                            activeSoldier.PerformLoudAction(10);
                        }
                        menu.CloseMoveUI();
                    }
                    else
                        menu.OpenOvermoveUI("Warning: Landing is further than 3cm away from jump point.");
                }
                else
                    menu.OpenMoveToSameSpotUI();
            }
        }
        else
        {
            //get maxmove
            float maxMove;
            if (moveTypeDropdown.options[moveTypeDropdown.value].text.Contains("Full"))
                maxMove = activeSoldier.FullMove;
            else if (moveTypeDropdown.options[moveTypeDropdown.value].text.Contains("Half"))
                maxMove = activeSoldier.HalfMove;
            else
                maxMove = activeSoldier.TileMove;

            if (GetMoveLocation(out Tuple<Vector3, string> moveToLocation))
            {
                if (activeSoldier.X != moveToLocation.Item1.x || activeSoldier.Y != moveToLocation.Item1.y || activeSoldier.Z != moveToLocation.Item1.z)
                {
                    //skip supression check if it's already happened before, otherwise run it
                    if (!activeSoldier.IsSuppressed() || (activeSoldier.IsSuppressed() && (moveTypeDropdown.interactable == false || activeSoldier.SuppressionCheck())))
                    {
                        int distance = CalculateMoveDistance(moveToLocation.Item1);
                        if (force || distance <= maxMove)
                        {
                            if (CheckMP(1) && CheckAP(ap))
                                PerformMove(activeSoldier, ap, moveToLocation, meleeToggle.isOn, coverToggle.isOn, fallInput.text, false);
                            menu.CloseMoveUI();
                        }
                        else
                            menu.OpenOvermoveUI($"Warning: Proposed move is {distance - maxMove} cm over max ({distance}/{maxMove})");
                    }
                    else
                        menu.OpenSuppressionMoveUI();
                }
                else
                    menu.OpenMoveToSameSpotUI();
            }
            else
                print("Invalid Input");
        }
    }
    public void PerformMove(Soldier movingSoldier, int ap, Tuple<Vector3, string> moveToLocation, bool meleeToggle, bool coverToggle, string fallDistance, bool freeMove)
    {
        FileUtility.WriteToReport($"{movingSoldier.soldierName} moved to {moveToLocation}");

        int.TryParse(fallDistance, out int fallDistanceInt); 
        string launchMelee = string.Empty;
        DeductAP(ap);
        if (!freeMove)
            DeductMP(1);
        Vector3 oldPos = new(movingSoldier.X, movingSoldier.Y, movingSoldier.Z);
        tempMove = Tuple.Create(oldPos, movingSoldier.TerrainOn, ap, 1);
        movingSoldier.X = (int)moveToLocation.Item1.x;
        movingSoldier.Y = (int)moveToLocation.Item1.y;
        movingSoldier.Z = (int)moveToLocation.Item1.z;
        movingSoldier.TerrainOn = moveToLocation.Item2;

        //activate in cover
        if (coverToggle)
            movingSoldier.SetCover();
        else
            movingSoldier.UnsetCover();

        //break melee control
        BreakAllControllingMeleeEngagments(movingSoldier);

        //unset overwatch
        movingSoldier.UnsetOverwatch();

        //break suppression
        movingSoldier.UnsetSuppression();

        //blow up claymores
        foreach (Claymore claymore in FindObjectsOfType<Claymore>())
            claymore.MoveOverClaymore(movingSoldier);

        //check for smoke clouds
        if (!movingSoldier.CheckSmokeClouds() && movingSoldier.IsInSmoke())
            movingSoldier.UnsetSmoked();

        //check for tabun clouds
        if (!movingSoldier.CheckTabunClouds() && movingSoldier.IsInTabun())
            movingSoldier.UnsetTabun();

        //check for fall damage
        if (fallDistanceInt > 0)
            movingSoldier.TakeDamage(movingSoldier, CalculateFallDamage(movingSoldier, fallDistanceInt), false, new() { "Fall" });

        //launch melee if melee toggle is on
        if (meleeToggle)
        {
            if (moveTypeDropdown.value == 0)
                launchMelee = "Full Charge Attack";
            else if (moveTypeDropdown.value == 1)
                launchMelee = "Half Charge Attack";
            else if (moveTypeDropdown.value == 2)
                launchMelee = "3cm Charge Attack";
        }

        //check broken soldier leaving field
        CheckBrokenFlee(movingSoldier);

        StartCoroutine(DetectionAlertSingle(movingSoldier, "losChange", oldPos, launchMelee, true));
    }
    public void CheckBrokenFlee(Soldier movingSoldier)
    {
        if (movingSoldier.IsBroken())
            if (movingSoldier.X == movingSoldier.startX && movingSoldier.Y == movingSoldier.startY && movingSoldier.Z == movingSoldier.startZ)
                menu.OpenBrokenFledUI();
    }
    public void CheckDeploymentBeacons(Soldier fieldedSoldier)
    {
        foreach (DeploymentBeacon beacon in FindObjectsOfType<DeploymentBeacon>())
            if (beacon.placedBy.IsSameTeamAs(fieldedSoldier) && fieldedSoldier.X == beacon.X && fieldedSoldier.Y == beacon.Y && fieldedSoldier.Z == beacon.Z)
                menu.AddXpAlert(beacon.placedBy, 1, $"Ally ({fieldedSoldier.soldierName}) deployed through beacon at ({beacon.X}, {beacon.Y}, {beacon.Z})", true);
    }











    //shot functions
    public void UpdateShotUI(Soldier shooter)
    {
        //if function is called not from a script, shooter has to be determined from interface
        if (shooter.id == "0")
            shooter = soldierManager.FindSoldierById(menu.shotUI.transform.Find("Shooter").GetComponent<TextMeshProUGUI>().text);

        if (!menu.clearShotFlag)
        {
            UpdateShotAP(shooter);
            if (shotTypeDropdown.value == 0)
                UpdateTarget(shooter);
            else
                UpdateSuppressionValue(shooter);
        }
    }
    public void UpdateShotType(Soldier shooter)
    {
        //if function is called not from a script, shooter has to be determined from interface
        if (shooter.id == "0")
            shooter = soldierManager.FindSoldierById(menu.shotUI.transform.Find("Shooter").GetComponent<TextMeshProUGUI>().text);

        TMP_Dropdown shotTypeDropdown = menu.shotUI.transform.Find("ShotType").Find("ShotTypeDropdown").GetComponent<TMP_Dropdown>();
        TMP_Dropdown targetDropdown = menu.shotUI.transform.Find("TargetPanel").Find("Target").Find("TargetDropdown").GetComponent<TMP_Dropdown>();
        List<TMP_Dropdown.OptionData> targetOptionDataList = new();

        //initialise
        targetDropdown.ClearOptions();
        menu.shotUI.transform.Find("Aim").gameObject.SetActive(false);
        menu.shotUI.transform.Find("SuppressionValue").gameObject.SetActive(false);
        menu.shotUI.transform.Find("TargetPanel").Find("CoverLocation").gameObject.SetActive(false);

        //generate target list
        foreach (Soldier s in AllSoldiers())
        {
            TMP_Dropdown.OptionData targetOptionData = null;
            if (s.IsAlive() && shooter.IsOppositeTeamAs(s) && s.IsRevealed())
            {
                if (shooter.CanSeeInOwnRight(s))
                    targetOptionData = new(s.Id, s.soldierPortrait);
                else
                    targetOptionData = new(s.Id, s.LoadPortraitTeamsight(s.soldierPortraitText));
            }

            if (targetOptionData != null)
            {
                targetOptionDataList.Add(targetOptionData);

                //remove option if target is jammer and shooter can't see in own right
                if (s.IsJammer() && !activeSoldier.CanSeeInOwnRight(s) && !shooter.IsRevoker())
                    targetOptionDataList.Remove(targetOptionData);

                //remove option if soldier is engaged and this soldier is not on the engagement list
                if (activeSoldier.IsMeleeEngaged() && !activeSoldier.IsMeleeEngagedWith(s))
                    targetOptionDataList.Remove(targetOptionData);
            }
        }

        if (shotTypeDropdown.value == 0)
        {
            menu.shotUI.transform.Find("Aim").gameObject.SetActive(true);

            //add explosive barrels to target list
            foreach (ExplosiveBarrel b in FindObjectsOfType<ExplosiveBarrel>())
            {
                TMP_Dropdown.OptionData targetOptionData = null;
                if (shooter.PhysicalObjectIsRevealed(b))
                    targetOptionData = new(b.Id, menu.explosiveBarrelSprite);

                if (targetOptionData != null)
                    targetOptionDataList.Add(targetOptionData);
            }

            //add coverman
            targetOptionDataList.Add(new("coverman", menu.covermanSprite));
        }
        else if (shotTypeDropdown.value == 1)
            menu.shotUI.transform.Find("SuppressionValue").gameObject.SetActive(true);

        targetDropdown.AddOptions(targetOptionDataList);
        UpdateShotUI(shooter);
    }
    public void UpdateShotAP(Soldier shooter)
    {
        int ap = 1;
        if (shotTypeDropdown.value == 1)
        {
            ap = shooter.ap;
        }
        else
        {
            if (aimTypeDropdown.value == 0)
            {
                if (shooter.IsGunner())
                    ap++;
                else
                {
                    ap += shooter.EquippedGun.SpecialityTag() switch
                    {
                        "Sniper Rifle" or "Light Machine Gun" => 2,
                        _ => 1,
                    };
                }
            }
        }

        //set ap to 0 for overwatch
        if (shooter.soldierTeam != currentTeam)
            ap = 0;
        
        menu.shotUI.transform.Find("APCost").Find("APCostDisplay").GetComponent<TextMeshProUGUI>().text = ap.ToString();
    }
    public void UpdateTarget(Soldier shooter)
    {
        IAmShootable target = FindShootableById(targetDropdown.options[targetDropdown.value].text);

        //initialise
        menu.shotUI.transform.Find("TargetPanel").Find("CoverLocation").gameObject.SetActive(false);
        menu.shotUI.transform.Find("TargetPanel").Find("Target").Find("BarrelLocation").gameObject.SetActive(false);
        menu.shotUI.transform.Find("TargetPanel").Find("CoverLevel").gameObject.SetActive(false);

        if (target is Coverman)
        {
            menu.shotUI.transform.Find("TargetPanel").Find("CoverLocation").gameObject.SetActive(true);
        }
        else if (target is ExplosiveBarrel targetBarrel)
        {
            menu.shotUI.transform.Find("TargetPanel").Find("Target").Find("BarrelLocation").GetComponent<TextMeshProUGUI>().text = $"X:{targetBarrel.X} Y:{targetBarrel.Y} Z:{targetBarrel.Z}";
            menu.shotUI.transform.Find("TargetPanel").Find("Target").Find("BarrelLocation").gameObject.SetActive(true);
        }
        else if (target is Soldier targetSoldier)
        {
            if (targetSoldier.IsInCover())
                menu.shotUI.transform.Find("TargetPanel").Find("CoverLevel").gameObject.SetActive(true);
                
            UpdateTargetFlanking(shooter, targetSoldier);
        }
    }
    public void UpdateSuppressionValue(Soldier shooter)
    {
        Item gun = shooter.EquippedGun;
        IAmShootable target = FindShootableById(targetDropdown.options[targetDropdown.value].text);

        int suppressionValue = CalculateRangeBracket(CalculateRange(shooter, target as PhysicalObject)) switch
        {
            "Melee" or "CQB" => gun.gunTraits.CQBSupPen,
            "Short" => gun.gunTraits.ShortSupPen,
            "Medium" => gun.gunTraits.MedSupPen,
            "Long" or "Coriolis" => gun.gunTraits.LongSupPen,
            _ => 0,
        };

        menu.shotUI.transform.Find("SuppressionValue").Find("SuppressionValueDisplay").GetComponent<TextMeshProUGUI>().text = suppressionValue.ToString();
    }
    public void UpdateTargetFlanking(Soldier shooter, Soldier target)
    {
        //clear the flanker ui
        menu.ClearFlankersUI(menu.flankersShotUI);
        int flankersCount = 0;
        int flankingAngle = 80;
        List<Tuple<float, Soldier>> allFlankingAngles = new();
        List<Tuple<float, Soldier>> confirmedFlankingAngles = new();

        if (shooter.IsTactician() && !target.IsRevoker())
            flankingAngle = 20;

        if (target != null)
        {
            if (!target.IsTactician() || shooter.IsRevoker())
            {
                //find all soldiers who could be considered for flanking and their flanking angles
                foreach (Soldier s in AllSoldiers())
                    if (s.IsAbleToSee() && s.IsSameTeamAs(shooter) && s.CanSeeInOwnRight(target) && !HelperFunctions.IsWithinAngle(new(shooter.X, shooter.Y), new(s.X, s.Y), new(target.X, target.Y), 80f))
                        allFlankingAngles.Add(Tuple.Create(HelperFunctions.CalculateAngle360(new(shooter.X, shooter.Y), new(s.X, s.Y), new(target.X, target.Y)), s));

                //order smallest angle to largest angle
                allFlankingAngles = allFlankingAngles.OrderBy(t => t.Item1).ToList();

                /*string msg = "";
                foreach (var item in allFlankingAngles)
                    msg += $"({item.Item1}, {item.Item2.soldierName})";
                print(msg);*/

                // Iterate through both lists to find out which gives more flanking options
                for (int i = 0; i < allFlankingAngles.Count; i++)
                {
                    Tuple<float, Soldier> currentItem = allFlankingAngles[i];
                    print($"current item {currentItem}");
                    // Check if Item1 is greater than flankingAngle
                    if (currentItem.Item1 > flankingAngle)
                    {
                        confirmedFlankingAngles.Add(currentItem);
                        allFlankingAngles = allFlankingAngles.Select(t => Tuple.Create(Math.Abs(t.Item1 - currentItem.Item1), t.Item2)).ToList();
                    }
                }

                foreach (Tuple<float, Soldier> confirmedFlankAngle in confirmedFlankingAngles)
                {
                    if (flankersCount < 3)
                    {
                        flankersCount++;

                        //add flanker to ui to visualise
                        GameObject flankerPortrait = Instantiate(menu.soldierPortraitPrefab, menu.flankersShotUI.transform.Find("FlankersPanel"));
                        flankerPortrait.GetComponentInChildren<SoldierPortrait>().Init(confirmedFlankAngle.Item2);
                    }
                }

                //display flankers if there are any
                if (flankersCount > 0)
                    menu.OpenFlankersUI(menu.flankersShotUI);
            }
        }
    }
    public int WeaponHitChance(Soldier shooter, IAmShootable target, Item gun)
    {
        int weaponHitChance, baseWeaponHitChance, sharpshooterBonus = 0, inspiredBonus = 0;

        //get base hit chance
        switch (CalculateRangeBracket(CalculateRange(shooter, target as PhysicalObject)))
        {
            case "Melee":
            case "CQB":
                if (aimTypeDropdown.value == 0)
                    baseWeaponHitChance = gun.gunTraits.CQBA;
                else
                    baseWeaponHitChance = gun.gunTraits.CQBU;
                break;
            case "Short":
                if (aimTypeDropdown.value == 0)
                    baseWeaponHitChance = gun.gunTraits.ShortA;
                else
                    baseWeaponHitChance = gun.gunTraits.ShortU;
                break;
            case "Medium":
                if (aimTypeDropdown.value == 0)
                    baseWeaponHitChance = gun.gunTraits.MedA;
                else
                    baseWeaponHitChance = gun.gunTraits.MedU;
                break;
            case "Long":
                if (aimTypeDropdown.value == 0)
                    baseWeaponHitChance = gun.gunTraits.LongA;
                else
                    baseWeaponHitChance = gun.gunTraits.LongU;
                break;
            case "Coriolis":
                if (aimTypeDropdown.value == 0)
                    baseWeaponHitChance = gun.gunTraits.CoriolisA;
                else
                    baseWeaponHitChance = gun.gunTraits.CoriolisU;
                break;
            default:
                baseWeaponHitChance = 0;
                break;
        }
        weaponHitChance = baseWeaponHitChance;

        //apply sharpshooter buff
        if (baseWeaponHitChance > 0 && shooter.IsSharpshooter() && target is Soldier targetSoldier && !targetSoldier.IsRevoker())
            sharpshooterBonus = 5;
        weaponHitChance += sharpshooterBonus;

        //apply inspirer buff
        inspiredBonus += shooter.InspirerBonusWeapon(gun);
        weaponHitChance += inspiredBonus;

        //correct negatives
        if (weaponHitChance < 0)
            weaponHitChance = 0;

        //report parameters
        shotParameters.Add(Tuple.Create("accuracy", $"{baseWeaponHitChance}"));
        shotParameters.Add(Tuple.Create("sharpshooter", $"{sharpshooterBonus}"));
        shotParameters.Add(Tuple.Create("inspired", $"{inspiredBonus}"));

        return weaponHitChance;
    }
    public float ShooterTraumaMod(Soldier shooter)
    {
        float traumaMod = shooter.tp switch
        {
            1 => 0.1f,
            2 => 0.2f,
            3 => 0.4f,
            _ => 0.0f,
        };

        //report parameters
        shotParameters.Add(Tuple.Create("trauma", $"{1 - traumaMod}"));

        return 1 - traumaMod;
    }
    public float ShooterSustenanceMod(Soldier shooter)
    {
        float sustenanceMod = 0;

        if (shooter.roundsWithoutFood >= 20)
            sustenanceMod = 0.5f;

        //report parameters
        shotParameters.Add(Tuple.Create("sustenance", $"{1 - sustenanceMod}"));

        return 1 - sustenanceMod;
    }
    public float ShooterSmokeMod(Soldier shooter)
    {
        float smokeMod = 0;

        if (shooter.IsSmokeBlinded())
            smokeMod = 0.4f;

        //report parameters
        shotParameters.Add(Tuple.Create("smoke", $"{1 - smokeMod}"));

        return 1 - smokeMod;
    }
    public float ShooterTabunMod(Soldier shooter)
    {
        float tabunMod = 0;

        if (shooter.IsInTabun())
        {
            if (shooter.CheckTabunEffectLevel(100))
                tabunMod = 0.8f;
            else if (shooter.CheckTabunEffectLevel(50))
                tabunMod = 0.4f;
            else if (shooter.CheckTabunEffectLevel(25))
                tabunMod = 0.2f;
        }

        //report parameters
        shotParameters.Add(Tuple.Create("tabun", $"{1 - tabunMod}"));

        return 1 - tabunMod;
    }
    public float RelevantWeaponSkill(Soldier shooter, Item gun)
    {
        int juggernautBonus = 0, stimBonus = 0;

        float weaponSkill = gun.SpecialityTag() switch
        {
            "Assault Rifle" => shooter.stats.AR.Val,
            "Light Machine Gun" => shooter.stats.LMG.Val,
            "Rifle" => shooter.stats.Ri.Val,
            "Shotgun" => shooter.stats.Sh.Val,
            "Sub-Machine Gun" => shooter.stats.SMG.Val,
            "Sniper Rifle" => shooter.stats.Sn.Val,
            "Pistol" or _ => shooter.stats.GetHighestWeaponSkill(),
        };

        //apply juggernaut armour debuff
        if (shooter.IsWearingJuggernautArmour(false))
            juggernautBonus = -1;
        weaponSkill += juggernautBonus;

        //apply stim armour buff
        if (shooter.IsWearingStimulantArmour())
            stimBonus = 2;
        weaponSkill += stimBonus;

        //apply trauma debuff
        weaponSkill *= ShooterTraumaMod(shooter);

        //apply sustenance debuff
        weaponSkill *= ShooterSustenanceMod(shooter);

        //correct negatives
        if (weaponSkill < 0)
            weaponSkill = 0;

        //report parameters
        shotParameters.Add(Tuple.Create("WS", $"{weaponSkill}"));
        shotParameters.Add(Tuple.Create("juggernaut", $"{juggernautBonus}"));
        shotParameters.Add(Tuple.Create("stim", $"{stimBonus}"));

        return weaponSkill;
    }
    public int TargetEvasion(IAmShootable target)
    {
        int targetE = 0;
        if (target is Soldier targetSoldier)
            targetE = targetSoldier.stats.E.Val;
        shotParameters.Add(Tuple.Create("tE", $"{targetE}"));
        return targetE;
    }
    public float CoverMod()
    {
        var coverMod = coverLevelDropdown.value switch
        {
            1 => 0.1f,
            2 => 0.34f,
            3 => 0.62f,
            4 => 0.88f,
            _ => 0.0f,
        };

        //report parameters
        shotParameters.Add(Tuple.Create("cover", $"{1 - coverMod}"));
        return 1 - coverMod;
    }
    public float VisMod(Soldier shooter)
    {
        float visMod;

        if (shooter.IsWearingThermalGoggles())
            visMod = 0.0f;
        else
        {
            visMod = weather.CurrentVis switch
            {
                "Full" => 0.0f,
                "Good" => 0.02f,
                "Moderate" => 0.18f,
                "Poor" => 0.26f,
                "Zero" => 0.64f,
                _ => 0.0f,
            };
        }

        //report parameters
        shotParameters.Add(Tuple.Create("vis", $"{1 - visMod}"));
        return 1 - visMod;
    }
    public float RainMod(Soldier shooter, IAmShootable target)
    {
        string rainfall = weather.CurrentRain;

        if (shooter.IsCalculator() && target is Soldier targetSoldier1 && !targetSoldier1.IsRevoker())
            rainfall = weather.DecreasedRain(rainfall);
        if (target is Soldier targetSoldier2 && targetSoldier2.IsCalculator() && !shooter.IsRevoker())
            rainfall = weather.IncreasedRain(rainfall);

        float rainMod = rainfall switch
        {
            "Zero" or "Light" => 0.0f,
            "Moderate" => 0.02f,
            "Heavy" => 0.08f,
            "Torrential" => 0.18f,
            _ => 0.0f,
        };

        //report parameters
        shotParameters.Add(Tuple.Create("rain", $"{1 - rainMod}"));
        return 1 - rainMod;
    }
    public float RainMod(Soldier shooter)
    {
        string rainfall = weather.CurrentRain;

        if (shooter.IsCalculator())
            rainfall = weather.DecreasedRain(rainfall);

        var rainMod = rainfall switch
        {
            "Zero" or "Light" => 0.0f,
            "Moderate" => 0.02f,
            "Heavy" => 0.08f,
            "Torrential" => 0.18f,
            _ => 0.0f,
        };

        //report parameters
        shotParameters.Add(Tuple.Create("rain", $"{1 - rainMod}"));
        return 1 - rainMod;
    }
    public float WindMod(Soldier shooter, IAmShootable target)
    {
        Vector2 shotLine = new(target.X - shooter.X, target.Y - shooter.Y);
        shotLine.Normalize();
        Vector2 windLine = weather.CurrentWindDirection;
        float shotAngleRelativeToWind = Vector2.Angle(shotLine, windLine);
        //print("WIND: " + windLine + " SHOT: " + shotLine + "ANGLE: " + shotAngleRelativeToWind);

        float windMod;

        string windSpeed = weather.CurrentWindSpeed;
        if (shooter.IsCalculator())
            windSpeed = weather.DecreasedWindspeed(windSpeed);
        if (target is Soldier targetSoldier)
            if (targetSoldier.IsCalculator())
                windSpeed = weather.IncreasedWindspeed(windSpeed);

        if (shotAngleRelativeToWind <= 22.5 || shotAngleRelativeToWind >= 157.5)
            windMod = 0f;
        else if (shotAngleRelativeToWind >= 67.5 && shotAngleRelativeToWind <= 112.5)
        {

            if (windSpeed == "Strong")
                windMod = 0.29f;
            else if (windSpeed == "Moderate")
                windMod = 0.12f;
            else if (windSpeed == "Light")
                windMod = 0.06f;
            else
                windMod = 0f;
        }
        else
        {
            if (windSpeed == "Strong")
                windMod = 0.10f;
            else if (windSpeed == "Moderate")
                windMod = 0.06f;
            else if (windSpeed == "Light")
                windMod = 0.02f;
            else
                windMod = 0f;
        }

        //report parameters
        shotParameters.Add(Tuple.Create("wind", $"{1 - windMod}"));
        return 1 - windMod;
    }
    public float ShooterHealthMod(Soldier shooter)
    {
        float shooterHealthMod;
        if (shooter.IsLastStand())
            shooterHealthMod = 0.6f;
        else if (shooter.hp <= shooter.stats.H.Val / 2)
            shooterHealthMod = 0.16f;
        else if (shooter.hp < shooter.stats.H.Val)
            shooterHealthMod = 0.06f;
        else
            shooterHealthMod = 0f;

        //report parameters
        shotParameters.Add(Tuple.Create("HP", $"{1 - shooterHealthMod}"));

        return 1 - shooterHealthMod;
    }
    public float TargetHealthMod(IAmShootable target)
    {
        float targetHealthMod = 0;
        if (target is Soldier targetSoldier)
        {
            
            if (targetSoldier.IsLastStand())
                targetHealthMod = -0.4f;
            else if (targetSoldier.hp <= targetSoldier.stats.H.Val / 2)
                targetHealthMod = -0.14f;
            else if (targetSoldier.hp < targetSoldier.stats.H.Val)
                targetHealthMod = -0.04f;
            else
                targetHealthMod = 0f;
        }
        //report parameters
        shotParameters.Add(Tuple.Create("tHP", $"{1 - targetHealthMod}"));
        return 1 - targetHealthMod;
    }
    public float ShooterTerrainMod(Soldier shooter)
    {
        float shooterTerrainMod;
        if (shooter.IsOnNativeTerrain())
            shooterTerrainMod = -0.04f;
        else if (shooter.IsOnOppositeTerrain())
            shooterTerrainMod = 0.12f;
        else
            shooterTerrainMod = 0.06f;

        //report parameters
        shotParameters.Add(Tuple.Create("Ter", $"{1 - shooterTerrainMod}"));

        return 1 - shooterTerrainMod;
    }
    public float TargetTerrainMod(IAmShootable target)
    {
        float targetTerrainMod = 0;
        if (target is Soldier targetSoldier)
        {
            if (targetSoldier.IsOnNativeTerrain())
                targetTerrainMod = 0.16f;
            else if (targetSoldier.IsOnOppositeTerrain())
                targetTerrainMod = -0.08f;
            else
                targetTerrainMod = -0.02f;
        }

        //report parameters
        shotParameters.Add(Tuple.Create("tTer", $"{1 - targetTerrainMod}"));
        return 1 - targetTerrainMod;
    }
    public float ElevationMod(Soldier shooter, IAmShootable target)
    {
        float elevationMod = (target.Z - shooter.Z) * 0.01f;

        //report parameters
        shotParameters.Add(Tuple.Create("elevation", $"{1 - elevationMod}"));
        return 1 - elevationMod;
    }
    public float KdMod(Soldier shooter)
    {
        float kdMod;
        int kd = shooter.GetKd();

        if (kd != 0)
            kdMod = -(2 * kd * 0.01f);
        else
            kdMod = 0;

        //report parameters
        shotParameters.Add(Tuple.Create("kd", $"{1 - kdMod}"));

        return 1 - kdMod;
    }
    public float OverwatchMod(Soldier shooter)
    {
        float overwatchMod;
        if (shooter.IsOnOverwatch())
        {
            if (shooter.IsGuardsman())
                overwatchMod = 0.2f;
            else
                overwatchMod = 0.4f;
        }
        else
            overwatchMod = 0;

        //report parameters
        shotParameters.Add(Tuple.Create("overwatch", $"{1 - overwatchMod}"));

        return 1 - overwatchMod;
    }
    public float FlankingMod(IAmShootable target)
    {
        float flankingMod = 0;
        if (target is Soldier targetSoldier)
        {
            if (!targetSoldier.IsTactician())
            {
                flankingMod = menu.flankersShotUI.transform.Find("FlankersPanel").childCount switch
                {
                    1 => -0.2f,
                    2 => -0.5f,
                    3 => -1.0f,
                    _ => 0f,
                };
            }
        }

        //report parameters
        shotParameters.Add(Tuple.Create("flank", $"{1 - flankingMod}"));
        return 1 - flankingMod;
    }
    public float StealthMod(Soldier shooter)
    {
        float stealthMod;
        if (shooter.IsHidden())
            stealthMod = -0.4f;
        else
            stealthMod = 0;

        //report parameters
        shotParameters.Add(Tuple.Create("stealth", $"{1 - stealthMod}"));

        return 1 - stealthMod;
    }
    public int ShooterSuppressionMod(Soldier shooter)
    {
        int suppressionMod = shooter.GetSuppression();
        
        //report parameters
        shotParameters.Add(Tuple.Create("suppression", $"{suppressionMod}"));

        return suppressionMod;
    }
    public Tuple<int, int, int> CalculateHitPercentage(Soldier shooter, IAmShootable target, Item gun)
    {
        //destroy old shot parameters
        shotParameters.Clear();
        Tuple<int, int, int> chances;
        int suppressedHitChance, hitChance, critChance;

        //calculate normal shot chance
        hitChance = Mathf.RoundToInt((WeaponHitChance(shooter, target, gun) + 10 * RelevantWeaponSkill(shooter, gun) - 12 * TargetEvasion(target)) * CoverMod() * VisMod(shooter) * RainMod(shooter, target) * WindMod(shooter, target) * ShooterHealthMod(shooter) * TargetHealthMod(target) * ShooterTerrainMod(shooter) * TargetTerrainMod(target) * ElevationMod(shooter, target) * KdMod(shooter) * OverwatchMod(shooter) * FlankingMod(target) * StealthMod(shooter) * ShooterSmokeMod(shooter) * ShooterTabunMod(shooter));

        //declare suppression shot chance
        suppressedHitChance = hitChance - ShooterSuppressionMod(shooter);

        //calculate critical shot chance
        critChance = Mathf.RoundToInt((Mathf.Pow(RelevantWeaponSkill(shooter, gun), 2) * (hitChance / 100.0f)) - TargetEvasion(target));

        //cap extremes
        if (suppressedHitChance < 0)
            suppressedHitChance = 0;
        else if (suppressedHitChance > 100)
            suppressedHitChance = 100;

        if (hitChance < 0)
            hitChance = 0;
        else if (hitChance > 100)
            hitChance = 100;

        if (critChance < 0)
            critChance = 0;
        else if (critChance > 100)
            critChance = 100;

        chances = Tuple.Create(hitChance, critChance, suppressedHitChance);

        return chances;
    }
    public void ConfirmShot(bool retry)
    {
        Soldier shooter = soldierManager.FindSoldierById(menu.shotUI.transform.Find("Shooter").GetComponent<TextMeshProUGUI>().text);
        IAmShootable target = FindShootableById(targetDropdown.options[targetDropdown.value].text);
        Item gun = shooter.EquippedGun;
        int.TryParse(menu.shotUI.transform.Find("APCost").Find("APCostDisplay").GetComponent<TextMeshProUGUI>().text, out int ap);
        int actingHitChance;
        bool resistSuppression;
        menu.shotResultUI.transform.Find("OptionPanel").Find("SuppressionResult").gameObject.SetActive(false);
        menu.shotResultUI.transform.Find("OptionPanel").Find("LosCheck").gameObject.SetActive(false);
        menu.shotResultUI.transform.Find("OptionPanel").Find("AvengerRetry").gameObject.SetActive(false);
        menu.shotResultUI.transform.Find("OptionPanel").Find("GuardsmanRetry").gameObject.SetActive(false);

        tempShooterTarget = Tuple.Create(shooter, target);
        menu.SetShotResolvedFlagTo(false);
        DeductAP(ap);

        if (shotTypeDropdown.value == 0) //standard shot
        {
            if (target is Soldier)
                FileUtility.WriteToReport($"{shooter.soldierName} shooting at {(target as Soldier).soldierName}");
            else
                FileUtility.WriteToReport($"{shooter.soldierName} shooting at {target.GetType()}");

            resistSuppression = shooter.SuppressionCheck();
            gun.SpendSingleAmmo();

            int randNum1 = RandomNumber(0, 100);
            int randNum2 = RandomNumber(0, 100);
            Tuple<int, int, int> chances;
            chances = CalculateHitPercentage(shooter, target, gun);

            //display shooter suppression indicator
            if (shooter.IsSuppressed())
            {
                menu.shotResultUI.transform.Find("OptionPanel").Find("SuppressionResult").gameObject.SetActive(true);

                if (resistSuppression)
                {
                    menu.shotResultUI.transform.Find("OptionPanel").Find("SuppressionResult").Find("ResultDisplay").GetComponent<TextMeshProUGUI>().text = "<color=green>Resisted Suppression</color>";
                    actingHitChance = chances.Item1;
                }
                else
                {
                    menu.shotResultUI.transform.Find("OptionPanel").Find("SuppressionResult").Find("ResultDisplay").GetComponent<TextMeshProUGUI>().text = "<color=orange>Suffered Suppression</color>";
                    actingHitChance = chances.Item3;
                }
            }
            else
                actingHitChance = chances.Item1;

            if (target is Coverman)
            {
                int coverDamage = CalculateRangeBracket(CalculateRange(shooter, target as PhysicalObject)) switch
                {
                    "Melee" or "CQB" => gun.gunTraits.CQBCovDamage,
                    "Short" => gun.gunTraits.ShortCovDamage,
                    "Medium" => gun.gunTraits.MedCovDamage,
                    "Long" or "Coriolis" => gun.gunTraits.LongCovDamage,
                    _ => 0,
                };

                //show los check button
                menu.shotResultUI.transform.Find("OptionPanel").Find("LosCheck").gameObject.SetActive(true);

                //standard shot hits cover
                if (randNum1 <= actingHitChance)
                {
                    menu.shotResultUI.transform.Find("OptionPanel").Find("ScatterResult").Find("ResultDisplay").GetComponent<TextMeshProUGUI>().text = "Shot directly on target.";

                    //critical shot hits cover
                    if (randNum2 <= chances.Item2)
                        menu.shotResultUI.transform.Find("OptionPanel").Find("Result").Find("ResultDisplay").GetComponent<TextMeshProUGUI>().text = "<color=green> COVER DESTROYED </color>";
                    else
                        menu.shotResultUI.transform.Find("OptionPanel").Find("Result").Find("ResultDisplay").GetComponent<TextMeshProUGUI>().text = "<color=green> Cover hit (" + coverDamage + " damage)</color>";

                }
                else
                {
                    menu.shotResultUI.transform.Find("OptionPanel").Find("Result").Find("ResultDisplay").GetComponent<TextMeshProUGUI>().text = "Miss";
                    menu.shotResultUI.transform.Find("OptionPanel").Find("ScatterResult").Find("ResultDisplay").GetComponent<TextMeshProUGUI>().text = $"Missed by {RandomShotScatterDistance()}cm {RandomShotScatterHorizontal()}, {RandomShotScatterDistance()}cm {RandomShotScatterVertical()}.\n\nDamage event ({gun.gunTraits.Damage}) on alternate target, or cover damage {gun.DisplayGunCoverDamage()}.";
                }
            }
            else if (target is ExplosiveBarrel targetBarrel)
            {
                //show los check button
                menu.shotResultUI.transform.Find("OptionPanel").Find("LosCheck").gameObject.SetActive(true);

                //standard shot hits barrel
                if (randNum1 <= actingHitChance)
                {
                    menu.shotResultUI.transform.Find("OptionPanel").Find("ScatterResult").Find("ResultDisplay").GetComponent<TextMeshProUGUI>().text = "Shot directly on target.";

                    //critical shot hits barrel
                    if (randNum2 <= chances.Item2)
                        menu.shotResultUI.transform.Find("OptionPanel").Find("Result").Find("ResultDisplay").GetComponent<TextMeshProUGUI>().text = "<color=green>Barrel Explodes (Crit)!</color>";
                    else
                        menu.shotResultUI.transform.Find("OptionPanel").Find("Result").Find("ResultDisplay").GetComponent<TextMeshProUGUI>().text = "<color=green>Barrel Explodes!</color>";
                    targetBarrel.CheckExplosionBarrel(shooter);
                }
                else
                {
                    menu.shotResultUI.transform.Find("OptionPanel").Find("Result").Find("ResultDisplay").GetComponent<TextMeshProUGUI>().text = "Miss";
                    menu.shotResultUI.transform.Find("OptionPanel").Find("ScatterResult").Find("ResultDisplay").GetComponent<TextMeshProUGUI>().text = $"Missed by {RandomShotScatterDistance()}cm {RandomShotScatterHorizontal()}, {RandomShotScatterDistance()}cm {RandomShotScatterVertical()}.\n\nDamage event ({gun.gunTraits.Damage}) on alternate target, or cover damage {gun.DisplayGunCoverDamage()}.";
                }
            }
            else if (target is Soldier targetSoldier) //check if target is soldier
            {
                //standard shot hits
                if (randNum1 <= actingHitChance)
                {
                    Soldier originalTarget = targetSoldier;

                    if (targetSoldier.IsMeleeEngaged()) //pick random target to hit in engagement
                    {
                        int randNum3 = RandomNumber(0, originalTarget.EngagedSoldiers.Count);
                        if (randNum3 > 0)
                        {
                            targetSoldier = originalTarget.EngagedSoldiers[randNum3 - 1];
                            menu.shotResultUI.transform.Find("OptionPanel").Find("ScatterResult").Find("ResultDisplay").GetComponent<TextMeshProUGUI>().text = $"Shot into melee aiming for {originalTarget.soldierName}, hit {targetSoldier.soldierName}.";
                        }
                        else
                            menu.shotResultUI.transform.Find("OptionPanel").Find("ScatterResult").Find("ResultDisplay").GetComponent<TextMeshProUGUI>().text = $"Shot into melee and hit intended target.";
                    }
                    else
                        menu.shotResultUI.transform.Find("OptionPanel").Find("ScatterResult").Find("ResultDisplay").GetComponent<TextMeshProUGUI>().text = "Shot directly on target.";

                    //standard shot crit hits
                    if (randNum2 <= chances.Item2)
                    {
                        targetSoldier.TakeDamage(shooter, gun.gunTraits.CritDamage, false, new() { "Critical", "Shot" });
                        menu.shotResultUI.transform.Find("OptionPanel").Find("Result").Find("ResultDisplay").GetComponent<TextMeshProUGUI>().text = "<color=green> CRITICAL SHOT </color>";

                        if (targetSoldier.IsSelf(originalTarget)) //only pay xp if you hit correct target 
                        {
                            //paying xp for hit
                            if (chances.Item1 >= 10)
                                menu.AddXpAlert(shooter, 8, $"Critical shot on {targetSoldier.soldierName}!", false);
                            else
                                menu.AddXpAlert(shooter, 10, $"Critical shot with a {chances.Item1}% chance on {targetSoldier.soldierName}!", false);
                        }
                    }
                    else
                    {
                        targetSoldier.TakeDamage(shooter, gun.gunTraits.Damage, false, new() { "Shot" });
                        menu.shotResultUI.transform.Find("OptionPanel").Find("Result").Find("ResultDisplay").GetComponent<TextMeshProUGUI>().text = "<color=green> Hit </color>";

                        if (targetSoldier.IsSelf(originalTarget)) //only pay xp if you hit correct target 
                        {
                            //paying xp for hit
                            if (chances.Item1 >= 10)
                                menu.AddXpAlert(shooter, 2, $"Shot hit on {targetSoldier.soldierName}.", false);
                            else
                                menu.AddXpAlert(shooter, 10, $"Shot hit with a {chances.Item1}% chance on {targetSoldier.soldierName}!", false);
                        }
                    }
                }
                else
                {
                    menu.shotResultUI.transform.Find("OptionPanel").Find("Result").Find("ResultDisplay").GetComponent<TextMeshProUGUI>().text = "Miss";
                    menu.shotResultUI.transform.Find("OptionPanel").Find("ScatterResult").Find("ResultDisplay").GetComponent<TextMeshProUGUI>().text = $"Missed by {RandomShotScatterDistance()}cm {RandomShotScatterHorizontal()}, {RandomShotScatterDistance()}cm {RandomShotScatterVertical()}.\n\nDamage event ({gun.gunTraits.Damage}) on alternate target, or cover damage {gun.DisplayGunCoverDamage()}.";
                    //show los check button if shot misses
                    menu.shotResultUI.transform.Find("OptionPanel").Find("LosCheck").gameObject.SetActive(true);

                    //show avenger retry if opponent has killed
                    if (targetSoldier.hasKilled && shooter.EquippedGun.CheckAnyAmmo() && !retry && !targetSoldier.IsRevoker())
                        menu.shotResultUI.transform.Find("OptionPanel").Find("AvengerRetry").gameObject.SetActive(true);

                    //paying xp for dodge
                    if (chances.Item1 <= 90)
                        menu.AddXpAlert(targetSoldier, 1, $"Dodged shot from {shooter.soldierName}.", false);
                    else
                        menu.AddXpAlert(targetSoldier, 10, $"Dodged shot with a {chances.Item1}% chance from {shooter.soldierName}!", false);

                    //push the no damage attack through for abilities trigger
                    targetSoldier.TakeDamage(shooter, 0, true, new() { "Shot" });
                }
            }
        }
        else if (shotTypeDropdown.value == 1) //supression shot
        {
            FileUtility.WriteToReport($"{shooter.soldierName} suppressing {target}");

            gun.SpendSpecificAmmo(gun.gunTraits.SuppressDrain, true);

            int suppressionValue = CalculateRangeBracket(CalculateRange(shooter, target as PhysicalObject)) switch
            {
                "Melee" or "CQB" => gun.gunTraits.CQBSupPen,
                "Short" => gun.gunTraits.ShortSupPen,
                "Medium" => gun.gunTraits.MedSupPen,
                "Long" => gun.gunTraits.LongSupPen,
                "Coriolis" => gun.gunTraits.CorSupPen,
                _ => 0,
            };
            (target as Soldier).SetSuppression(suppressionValue);
            menu.shotResultUI.transform.Find("OptionPanel").Find("Result").Find("ResultDisplay").GetComponent<TextMeshProUGUI>().text = $"<color=green> Supressing ({suppressionValue})</color>";
            menu.shotResultUI.transform.Find("OptionPanel").Find("ScatterResult").Find("ResultDisplay").GetComponent<TextMeshProUGUI>().text = $"Suppressing {(target as Soldier).soldierName} until next round.";
        }

        //trigger loud action
        shooter.PerformLoudAction();

        menu.OpenShotResultUI();
        menu.CloseShotUI();
    }


















    //melee functions
    public void UpdateMeleeUI()
    {
        Soldier attacker = soldierManager.FindSoldierById(menu.meleeUI.transform.Find("Attacker").GetComponent<TextMeshProUGUI>().text);
        Soldier defender = soldierManager.FindSoldierByName(meleeTargetDropdown.options[meleeTargetDropdown.value].text);

        if (!menu.clearMeleeFlag)
        {
            UpdateMeleeAP(attacker);
            UpdateMeleeDefenderWeapon(defender);
            UpdateMeleeFlankingAgainstAttacker(attacker, defender);
            UpdateMeleeFlankingAgainstDefender(attacker, defender);
        }
            
    }
    public void UpdateMeleeAP(Soldier attacker)
    {
        if (meleeTypeDropdown.options[0].text == "Static Attack")
        {
            if (attacker.IsFighter())
                menu.meleeUI.transform.Find("APCost").Find("APCostDisplay").GetComponent<TextMeshProUGUI>().text = "1";
            else
                menu.meleeUI.transform.Find("APCost").Find("APCostDisplay").GetComponent<TextMeshProUGUI>().text = "2";
        }
        else
            menu.meleeUI.transform.Find("APCost").Find("APCostDisplay").GetComponent<TextMeshProUGUI>().text = "0";
    }
    public void UpdateMeleeDefenderWeapon(Soldier defender)
    {
        Image defenderWeaponImage = menu.meleeUI.transform.Find("TargetPanel").Find("DefenderWeapon").Find("WeaponImage").GetComponent<Image>();

        //show defender weapon
        if (defender.BestMeleeWeapon != null)
            defenderWeaponImage.sprite = defender.BestMeleeWeapon.itemImage;
        else
            defenderWeaponImage.sprite = menu.fist;
    }
    public void UpdateMeleeFlankingAgainstAttacker(Soldier attacker, Soldier defender)
    {
        //clear the flanker ui
        menu.ClearFlankersUI(menu.flankersMeleeAttackerUI);
        int flankersCount = 0;
        foreach (Soldier s in AllSoldiers())
        {
            if (s.IsAbleToSee() && s.IsOppositeTeamAs(attacker) && !s.IsSelf(defender) && s.PhysicalObjectWithinMeleeRadius(attacker))
            {
                //add flanker to ui to visualise
                flankersCount++;
                GameObject flankerPortrait = Instantiate(menu.possibleFlankerPrefab, menu.flankersMeleeAttackerUI.transform.Find("FlankersPanel"));
                flankerPortrait.GetComponentInChildren<SoldierPortrait>().Init(s);
            }
        }

        //display flankers if there are any
        if (flankersCount > 0)
            menu.OpenFlankersUI(menu.flankersMeleeAttackerUI);
    }
    public void UpdateMeleeFlankingAgainstDefender(Soldier attacker, Soldier defender)
    {
        //clear the flanker ui
        menu.ClearFlankersUI(menu.flankersMeleeDefenderUI);
        int flankersCount = 0;
        if (!defender.IsTactician() || attacker.IsRevoker())
        {
            foreach (Soldier s in AllSoldiers())
            {
                if (s.IsAbleToSee() && s.IsOppositeTeamAs(defender) && !s.IsSelf(attacker) && s.PhysicalObjectWithinMeleeRadius(defender) && flankersCount < 3)
                {
                    flankersCount++;

                    //add flanker to ui to visualise
                    GameObject flankerPortrait = Instantiate(menu.possibleFlankerPrefab, menu.flankersMeleeDefenderUI.transform.Find("FlankersPanel"));
                    flankerPortrait.GetComponentInChildren<SoldierPortrait>().Init(s);
                }
            }

            //display flankers if there are any
            if (flankersCount > 0)
                menu.OpenFlankersUI(menu.flankersMeleeDefenderUI);
        }
    }
    public void UpdateMeleeTypeOptions()
    {
        Soldier attacker = soldierManager.FindSoldierById(menu.meleeUI.transform.Find("Attacker").GetComponent<TextMeshProUGUI>().text);
        Soldier defender = soldierManager.FindSoldierByName(meleeTargetDropdown.options[meleeTargetDropdown.value].text);

        List<TMP_Dropdown.OptionData> meleeTypeDetails = new()
        {
            new TMP_Dropdown.OptionData(menu.meleeChargeIndicator),
            new TMP_Dropdown.OptionData("Engagement Only"),
        };

        if (defender.controlledBySoldiersList.Contains(attacker.id))
            meleeTypeDetails.Add(new TMP_Dropdown.OptionData("<color=green>Disengage</color>"));
        else if (defender.controllingSoldiersList.Contains(attacker.id))
            meleeTypeDetails.Add(new TMP_Dropdown.OptionData("<color=red>Request Disengage</color>"));

        meleeTypeDropdown.ClearOptions();
        meleeTypeDropdown.AddOptions(meleeTypeDetails);
    }
    public float AttackerMeleeSkill(Soldier attacker)
    {
        int juggernautBonus = 0;
        float inspirerBonus, attackerMeleeSkill = attacker.stats.M.Val;

        //apply JA debuff
        if (attacker.IsWearingJuggernautArmour(false))
            juggernautBonus = -1;
        attackerMeleeSkill += juggernautBonus;

        //check for inspirer
        inspirerBonus = attacker.InspirerBonusWeaponMelee();
        attackerMeleeSkill += inspirerBonus;

        //apply sustenance debuff
        attackerMeleeSkill *= AttackerSustenanceMod(attacker);

        //correct negatives
        if (attackerMeleeSkill < 0)
            attackerMeleeSkill = 0;

        meleeParameters.Add(Tuple.Create("aM", $"{attacker.stats.M.Val}"));
        meleeParameters.Add(Tuple.Create("aJuggernaut", $"{juggernautBonus}"));
        meleeParameters.Add(Tuple.Create("aInspirer", $"{inspirerBonus}"));
        return attackerMeleeSkill;
    }
    public float AttackerSustenanceMod(Soldier attacker)
    {
        float sustenanceMod = 0;

        if (attacker.roundsWithoutFood >= 20)
            sustenanceMod = 0.5f;

        meleeParameters.Add(Tuple.Create("aSustenance", $"{1 - sustenanceMod}"));
        return 1 - sustenanceMod;
    }
    public int AttackerWeaponDamage(Item weapon)
    {
        int attackerWeaponDamage;

        //if fist selected
        if (weapon == null)
            attackerWeaponDamage = 1;
        else
            attackerWeaponDamage = weapon.meleeDamage;

        meleeParameters.Add(Tuple.Create("aWep", $"{attackerWeaponDamage}"));
        return attackerWeaponDamage;
    }
    public float AttackerHealthMod(Soldier attacker)
    {
        float attackerHealthMod;
        if (attacker.IsLastStand())
            attackerHealthMod = 0.6f;
        else if (attacker.hp <= attacker.stats.H.Val / 2)
            attackerHealthMod = 0.20f;
        else if (attacker.hp < attacker.stats.H.Val)
            attackerHealthMod = 0.06f;
        else
            attackerHealthMod = 0f;

        meleeParameters.Add(Tuple.Create("aHP", $"{1 - attackerHealthMod}"));
        return 1 - attackerHealthMod;
    }
    public float AttackerTerrainMod(Soldier attacker)
    {
        float attackerTerrainMod;
        if (attacker.IsOnNativeTerrain())
            attackerTerrainMod = -0.4f;
        else if (attacker.IsOnOppositeTerrain())
            attackerTerrainMod = 0.2f;
        else
            attackerTerrainMod = 0f;

        meleeParameters.Add(Tuple.Create("aTer", $"{1 - attackerTerrainMod}"));
        return 1 - attackerTerrainMod;
    }
    public float FlankingAgainstAttackerMod()
    {
        int flankersCount = 0;
        foreach (Transform child in menu.flankersMeleeAttackerUI.transform.Find("FlankersPanel"))
            if (child.GetComponentInChildren<Toggle>().isOn)
                flankersCount++;

        float attackerFlankingMod = flankersCount switch
        {
            0 => 0f,
            1 => 0.16f,
            2 => 0.46f,
            3 or _ => 0.8f
        };

        meleeParameters.Add(Tuple.Create("aFlank", $"{1 - attackerFlankingMod}"));
        return 1 - attackerFlankingMod;
    }
    public float AttackerStrengthMod(Soldier attacker)
    {
        float strengthMod = attacker.stats.Str.Val;
        strengthMod *= 0.2f;

        meleeParameters.Add(Tuple.Create("aStr", $"{attacker.stats.Str.Val}"));
        return strengthMod;
    }
    public float DefenderMeleeSkill(Soldier defender)
    {
        int juggernautBonus = 0;
        float defenderMeleeSkill = defender.stats.M.Val;

        //apply JA debuff
        if (defender.IsWearingJuggernautArmour(false))
            juggernautBonus = -1;
        defenderMeleeSkill += juggernautBonus;

        //apply sustenance debuff
        defenderMeleeSkill *= DefenderSustenanceMod(defender);

        //correct negatives
        if (defenderMeleeSkill < 0)
            defenderMeleeSkill = 0;

        meleeParameters.Add(Tuple.Create("dM", $"{defender.stats.M.Val}"));
        meleeParameters.Add(Tuple.Create("dJuggernaut", $"{juggernautBonus}"));
        return defenderMeleeSkill;
    }
    public float DefenderSustenanceMod(Soldier defender)
    {
        float sustenanceMod = 0;

        if (defender.roundsWithoutFood >= 20)
            sustenanceMod = 0.5f;

        meleeParameters.Add(Tuple.Create("dSustenance", $"{1 - sustenanceMod}"));
        return 1 - sustenanceMod;
    }
    public int DefenderWeaponDamage(Item weapon)
    {
        int defenderWeaponDamage;

        //if fist selected
        if (weapon == null)
            defenderWeaponDamage = 1;
        else
            defenderWeaponDamage = weapon.meleeDamage;

        meleeParameters.Add(Tuple.Create("dWep", $"{defenderWeaponDamage}"));
        return defenderWeaponDamage;
    }
    public float ChargeModifier()
    {
        float chargeMod;

        chargeMod = meleeTypeDropdown.options[meleeTypeDropdown.value].text switch
        {
            "Full Charge Attack" => 1.9f,
            "Half Charge Attack" => 1.4f,
            "3cm Charge Attack" => 1.1f,
            "Static Attack" or _ => 0f,
        };

        meleeParameters.Add(Tuple.Create("charge", $"{chargeMod}"));
        return chargeMod;
    }
    public float DefenderHealthMod(Soldier defender)
    {
        float defenderHealthMod;
        if (defender.IsLastStand())
            defenderHealthMod = 0.8f;
        else if (defender.hp <= activeSoldier.stats.H.Val / 2)
            defenderHealthMod = 0.4f;
        else if (defender.hp < activeSoldier.stats.H.Val)
            defenderHealthMod = 0.2f;
        else
            defenderHealthMod = 0f;

        meleeParameters.Add(Tuple.Create("dHP", $"{1 - defenderHealthMod}"));
        return 1 - defenderHealthMod;
    }
    public float DefenderTerrainMod(Soldier defender)
    {
        float defenderTerrainMod;
        if (defender.IsOnNativeTerrain())
            defenderTerrainMod = -0.4f;
        else if (defender.IsOnOppositeTerrain())
            defenderTerrainMod = 0.4f;
        else
            defenderTerrainMod = 0f;

        meleeParameters.Add(Tuple.Create("dTer", $"{1 - defenderTerrainMod}"));
        return 1 - defenderTerrainMod;
    }
    public float FlankingAgainstDefenderMod(Soldier defender)
    {
        float defenderFlankingMod = 0;
        if (!defender.IsTactician())
        {
            int flankersCount = 0;
            foreach (Transform child in menu.flankersMeleeDefenderUI.transform.Find("FlankersPanel"))
                if (child.GetComponentInChildren<Toggle>().isOn)
                    flankersCount++;

            defenderFlankingMod = flankersCount switch
            {
                0 => 0f,
                1 => 0.26f,
                2 => 0.56f,
                3 or _ => 0.86f
            };
        }

        meleeParameters.Add(Tuple.Create("dFlank", $"{1 - defenderFlankingMod}"));
        return 1 - defenderFlankingMod;
    }
    public float DefenderStrengthMod(Soldier defender)
    {
        float strengthMod = defender.stats.Str.Val;
        strengthMod *= 0.2f;

        meleeParameters.Add(Tuple.Create("dStr", $"{defender.stats.Str.Val}"));
        return strengthMod;
    }
    public float SuppressionMod(Soldier soldier)
    {
        float suppressionMod = soldier.GetSuppression() / 100f;

        meleeParameters.Add(Tuple.Create("suppression", $"{1 - suppressionMod}"));
        return 1 - suppressionMod;
    }
    public int CalculateMeleeResult(Soldier attacker, Soldier defender)
    {
        //destroy old melee parameters
        meleeParameters.Clear();

        float meleeDamage;
        int meleeDamageFinal;
        Item attackerWeapon = attacker.BestMeleeWeapon;
        Item defenderWeapon = defender.BestMeleeWeapon;
        int bloodrageMultiplier = 1;

        //if it's a normal attack
        if (meleeTypeDropdown.value == 0)
        {
            meleeDamage = (AttackerMeleeSkill(attacker) + AttackerWeaponDamage(attackerWeapon)) * AttackerHealthMod(attacker) * AttackerTerrainMod(attacker) * KdMod(attacker) * FlankingAgainstAttackerMod() * SuppressionMod(attacker) + AttackerStrengthMod(attacker) - ((DefenderMeleeSkill(defender) + DefenderWeaponDamage(defenderWeapon) + ChargeModifier()) * DefenderHealthMod(defender) * DefenderTerrainMod(defender) * FlankingAgainstDefenderMod(defender) * SuppressionMod(defender) + DefenderStrengthMod(defender));

            //check bloodletter damage bonus
            if (meleeDamage > 0 && attacker.IsBloodRaged() && !defender.IsRevoker())
                bloodrageMultiplier = 2;

            meleeParameters.Add(Tuple.Create("bloodrage", $"{bloodrageMultiplier}"));
            meleeDamage *= bloodrageMultiplier;
            

            //rounding based on R
            if (attacker.stats.R.Val > defender.stats.R.Val)
            {
                meleeParameters.Add(Tuple.Create("rounding", "Attacker favoured."));
                meleeDamageFinal = Mathf.CeilToInt(meleeDamage);
            }
            else if (attacker.stats.R.Val < defender.stats.R.Val)
            {
                meleeParameters.Add(Tuple.Create("rounding", "Defender favoured."));
                meleeDamageFinal = Mathf.FloorToInt(meleeDamage);
            }
            else
            {
                meleeParameters.Add(Tuple.Create("rounding", "Neither favoured."));
                meleeDamageFinal = Mathf.RoundToInt(meleeDamage);
            }
        }
        else
        {
            meleeParameters.Add(Tuple.Create("rounding", "N/A"));
            meleeDamageFinal = 0;
        }

        return meleeDamageFinal;
    }
    public void EstablishController(Soldier controller, Soldier s2)
    {
        BreakMeleeEngagement(controller, s2);
        controller.controllingSoldiersList.Add(s2.id);
        controller.controlledBySoldiersList.Remove(s2.id);
        s2.controlledBySoldiersList.Add(controller.id);
        s2.controllingSoldiersList.Remove(controller.id);

        menu.OpenMeleeResultUI();
    }
    public void EstablishNoController(Soldier s1, Soldier s2)
    {
        BreakMeleeEngagement(s1, s2);

        menu.OpenMeleeResultUI();
    }
    public string DetermineMeleeController(Soldier attacker, Soldier defender, bool counterattack, bool disengage)
    {
        string controlResult;

        if (defender.IsDead())
        {
            EstablishNoController(attacker, defender);
            controlResult = "<color=red>No-one Controlling\n(" + defender.soldierName + " Dead)</color>";
        }
        else if (attacker.IsDead())
        {
            EstablishNoController(attacker, defender);
            controlResult = "<color=red>No-one Controlling\n(" + attacker.soldierName + " Dead)</color>";
        }
        else if (!defender.IsConscious())
        {
            EstablishNoController(attacker, defender);
            controlResult = "<color=blue>No-one Controlling\n(" + defender.soldierName + " Unconscious)</color>";
        }
        else if (!attacker.IsConscious())
        {
            EstablishNoController(attacker, defender);
            controlResult = "<color=blue>No-one Controlling\n(" + attacker.soldierName + " Unconscious)</color>";
        }
        else if (defender.IsPlayingDead())
        {
            EstablishNoController(attacker, defender);
            controlResult = "<color=orange>No-one Controlling\n(" + defender.soldierName + " Playdead)</color>";
        }
        else if (attacker.IsPlayingDead())
        {
            EstablishNoController(attacker, defender);
            controlResult = "<color=orange>No-one Controlling\n(" + attacker.soldierName + " Playdead)</color>";
        }
        else if (defender.IsBroken())
        {
            EstablishNoController(attacker, defender);
            controlResult = "<color=orange>No-one Controlling\n(" + defender.soldierName + " Broken)</color>";
        }
        else if (counterattack)
        {
            EstablishNoController(attacker, defender);
            controlResult = "<color=orange>No-one Controlling\n(" + defender.soldierName + " Counterattacked)</color>";
        }
        else if (disengage)
        {
            EstablishNoController(attacker, defender);
            controlResult = "<color=orange>No-one Controlling\n(" + attacker.soldierName + " Disengaged)</color>";
        }
        else
        {
            if (attacker.stats.R.Val > defender.stats.R.Val)
            {
                EstablishController(attacker, defender);
                controlResult = "<color=green>" + attacker.soldierName + " Controlling\n(Higher R)</color>";
            }
            else if (attacker.stats.R.Val < defender.stats.R.Val)
            {
                EstablishController(defender, attacker);
                controlResult = "<color=red>" + defender.soldierName + " Controlling\n(Higher R)</color>";
            }
            else
            {
                if (attacker.stats.Str.Val > defender.stats.Str.Val)
                {
                    EstablishController(attacker, defender);
                    controlResult = "<color=green>" + attacker.soldierName + " Controlling\n(Higher Str)</color>";
                }
                else if (attacker.stats.Str.Val < defender.stats.Str.Val)
                {
                    EstablishController(defender, attacker);
                    controlResult = "<color=red>" + defender.soldierName + " Controlling\n(Higher Str)</color>";
                }
                else
                {
                    if (attacker.stats.M.Val > defender.stats.M.Val)
                    {
                        EstablishController(attacker, defender);
                        controlResult = "<color=green>" + attacker.soldierName + " Controlling\n(Higher M)</color>";
                    }
                    else if (attacker.stats.M.Val < defender.stats.M.Val)
                    {
                        EstablishController(defender, attacker);
                        controlResult = "<color=red>" + defender.soldierName + " Controlling\n(Higher M)</color>";
                    }
                    else
                    {
                        EstablishNoController(attacker, defender);
                        controlResult = "<color=orange>No-one Controlling\n(Evenly Matched)</color>";
                    }
                }
            }
        }
        return controlResult;
    }
    public void BreakAllControllingMeleeEngagments(Soldier s1)
    {
        List<string> engagedSoldiersList = new();

        foreach (string soldierId in s1.controllingSoldiersList)
            engagedSoldiersList.Add(soldierId);

        foreach (string soldierId in engagedSoldiersList)
            BreakMeleeEngagement(s1, soldierManager.FindSoldierById(soldierId));
    }
    public void BreakMeleeEngagement(Soldier s1, Soldier s2)
    {
        s1.controllingSoldiersList.Remove(s2.id);
        s1.controlledBySoldiersList.Remove(s2.id);
        s2.controllingSoldiersList.Remove(s1.id);
        s2.controlledBySoldiersList.Remove(s1.id);
    }
    public void ConfirmMelee()
    {
        Soldier attacker = soldierManager.FindSoldierById(menu.meleeUI.transform.Find("Attacker").GetComponent<TextMeshProUGUI>().text);
        Soldier defender = soldierManager.FindSoldierByName(meleeTargetDropdown.options[meleeTargetDropdown.value].text);

        if (int.TryParse(menu.meleeUI.transform.Find("APCost").Find("APCostDisplay").GetComponent<TextMeshProUGUI>().text, out int ap))
        {
            if (CheckAP(ap))
            {
                FileUtility.WriteToReport($"{attacker.soldierName} starting melee attack on {defender.soldierName}");

                //determine if damage is from melee or melee charge
                List<string> damageType = new() { "Melee"};
                if (ap == 0)
                    damageType.Add("Charge");

                menu.SetMeleeResolvedFlagTo(false);
                DeductAP(ap);

                int meleeDamage = CalculateMeleeResult(attacker, defender);
                string damageMessage;
                bool counterattack = false, instantKill = false, loudAction = true, disengage = false;

                //engagement only options
                if (meleeTypeDropdown.value == 1)
                {
                    damageMessage = "<color=orange>No Damage\n(Enagament Only)</color>";
                    //loudAction = false;
                }
                else if (meleeTypeDropdown.value == 2)
                {
                    damageMessage = "<color=orange>No Damage\n(Disengagement)</color>";
                    disengage = true;
                    //loudAction = false;
                }
                else
                {
                    //instant kill scenarios
                    if (attacker.IsHidden() && attacker.stats.F.Val > defender.stats.M.Val) //stealth kill
                    {
                        damageMessage = "<color=green>INSTANT KILL\n(Stealth Attack)</color>";
                        instantKill = true;
                        loudAction = false;
                    }
                    else if (defender.IsOnOverwatch() && attacker.stats.M.Val >= defender.stats.M.Val) //overwatch kill
                    {
                        damageMessage = "<color=green>INSTANT KILL\n(Overwatcher)</color>";
                        instantKill = true;
                    }
                    else if (!defender.IsConscious()) //unconscious kill
                    {
                        damageMessage = "<color=green>INSTANT KILL\n(Unconscious)</color>";
                        instantKill = true;
                    }
                    else if (defender.IsPlayingDead()) //playdead kill
                    {
                        damageMessage = "<color=green>INSTANT KILL\n(Playdead)</color>";
                        instantKill = true;
                    }
                    else
                    {
                        //melee attack proceeds
                        if (meleeDamage > 0)
                        {
                            if (attacker.IsWearingExoArmour() && !defender.IsWearingJuggernautArmour(false)) //exo kill on standard man
                            {
                                damageMessage = "<color=green>INSTANT KILL\n(Exo Armour)</color>";
                                instantKill = true;
                            }
                            else
                            {
                                if (defender.IsWearingJuggernautArmour(false) && !attacker.IsWearingExoArmour())
                                    damageMessage = "<color=orange>No Damage\n(Juggernaut Immune)</color>";
                                else
                                    damageMessage = "<color=green>Successful Attack\n(" + meleeDamage + " Damage)</color>";
                                defender.TakeDamage(attacker, meleeDamage, false, damageType);
                                attacker.FighterMeleeHitReward();
                            }
                        }
                        else if (meleeDamage < 0)
                        {
                            //play counterattack sound
                            soundManager.PlayCounterattack();

                            if (!defender.IsWearingExoArmour() && attacker.IsWearingJuggernautArmour(false)) //no damage counter against jugs
                                damageMessage = "<color=orange>No Damage\n(Juggernaut Immune)</color>";
                            else
                            {
                                counterattack = true;
                                meleeDamage *= -1;
                                damageMessage = "<color=red>Counterattacked\n(" + meleeDamage + " Damage)</color>";
                                attacker.TakeDamage(defender, meleeDamage, false, damageType);
                            }
                        }
                        else
                        {
                            damageMessage = "<color=orange>No Damage\n(Evenly Matched)</color>";
                            
                            //push the no damage attack through for abilities trigger
                            defender.TakeDamage(attacker, meleeDamage, true, damageType);
                        }
                    }

                    //reset bloodrage even if non-successful attack
                    attacker.UnsetBloodRage();
                }

                //add xp for successful melee attack
                if (meleeTypeDropdown.value == 0 && !damageMessage.Contains("No Damage"))
                {
                    if (counterattack)
                        menu.AddXpAlert(defender, 2 + meleeDamage, $"Melee counterattack attack on {attacker.soldierName} for {meleeDamage} damage.", false);
                    else
                    {
                        if (meleeDamage == 0)
                            menu.AddXpAlert(defender, 2, $"Melee block against {attacker.soldierName}.", false);
                        else
                            menu.AddXpAlert(attacker, 1, $"Successful melee attack on {defender.soldierName}.", false);
                    }
                }

                //kill if instantKill
                if (instantKill)
                    defender.InstantKill(attacker, new List<string>() { "Melee" });

                //attacker and defender exit cover
                attacker.UnsetCover();
                defender.UnsetCover();

                //attacker and defender exit overwatch
                attacker.UnsetOverwatch();
                defender.UnsetOverwatch();

                //add melee alert
                menu.AddMeleeAlert(attacker, defender, damageMessage, DetermineMeleeController(attacker, defender, counterattack, disengage));

                //trigger loud action
                if (loudAction)
                    attacker.PerformLoudAction();

                menu.OpenMeleeResultUI();
                menu.CloseMeleeUI();
            }
        }
    }
    public IEnumerator DetermineMeleeControllerMultiple(Soldier s1)
    {
        if (s1.EngagedSoldiers.Count > 0)
        {
            menu.SetMeleeResolvedFlagTo(false);
            yield return new WaitForSeconds(0.05f);
            foreach (Soldier s in s1.EngagedSoldiers)
                menu.AddMeleeAlert(s1, s, "No Damage\n(Engagement Change)", DetermineMeleeController(s1, s, false, false));
        }
    }














    //configure functions
    public int UpdateConfigureAP()
    {
        int totalDrop = 0, totalPickup = 0, totalSwap = 0, ap = 0;

        foreach (ItemIcon itemIcon in menu.configureUI.GetComponentsInChildren<ItemIcon>(true))
        {
            Item item = itemIcon.item;
            if (item.markedForAction != string.Empty)
            {
                string[] instructions = item.markedForAction.Split('|');
                if (instructions[0] == instructions[2] && instructions[1] != instructions[3])
                    totalSwap++;
                else if (instructions[2] == "none")
                {
                    if (!instructions[1].Contains("Hand"))
                        totalDrop++;
                }
                else
                    totalPickup++;

                if (item.equippableSlots.Contains("Head")) //is a special armour
                    ap = 3;
            }
        }
        print($"totaldrop|{totalDrop} totalswap|{totalSwap} totalpickup|{totalPickup}");

        //calculate ap required for action
        if (totalDrop > 2)
            ap += 2;
        else
            ap += totalDrop;

        ap += totalPickup;
        ap += totalSwap;

        if (ap > 3)
            ap = 3;

        //apply adept ap reduction
        if (activeSoldier.IsAdept() && ap > 1)
            ap -= 1;

        //configure free on first soldier turn is they haven't used ap
        if (activeSoldier.roundsFielded == 0 && !activeSoldier.usedAP)
            ap = 0;

        menu.configureUI.transform.Find("APCost").Find("APCostDisplay").GetComponent<TextMeshProUGUI>().text = ap.ToString();
        return ap;
    }
    public List<Item> FindNearbyItems()
    {
        List<Item> nearbyItems = new();
        foreach (Item i in FindObjectsOfType<Item>())
            if (activeSoldier.PhysicalObjectWithinItemRadius(i))
                nearbyItems.Add(i);

        return nearbyItems;
    }
    public void ConfirmConfigure()
    {
        int ap = UpdateConfigureAP();
        if (CheckAP(ap)) 
        {
            DeductAP(ap);
            foreach (ItemIcon itemIcon in menu.configureUI.GetComponentsInChildren<ItemIcon>(true))
            {
                Item item = itemIcon.item;
                if (item.markedForAction != string.Empty)
                {
                    string[] instructions = item.markedForAction.Split('|');
                    print(item.markedForAction);
                    item.MoveItem(FindHasInventoryById(instructions[0]), instructions[1], FindHasInventoryById(instructions[2]), instructions[3]);
                }
            }
            activeSoldier.PerformLoudAction(5);
            menu.CloseConfigureUI();
        }
    }
    //item functions
    public void ConfirmUseItem(UseItemUI useItemUI)
    {

        Item itemUsed = useItemUI.itemUsed;
        Item itemUsedOn = useItemUI.itemUsedOn;
        Soldier soldierUsedOn = useItemUI.soldierUsedOn;
        ItemIcon linkedIcon = useItemUI.itemUsedIcon;
        int ap = itemUsed.usageAP;
        //adept ability
        if (activeSoldier.IsAdept() && itemUsed.usageAP > 1)
            ap--;
        //gunner ability
        if (activeSoldier.IsGunner() && itemUsed.IsAmmo())
            ap = 1;

        FileUtility.WriteToReport($"{((Soldier)itemUsed.owner).soldierName} used {itemUsed.itemName}.");

        switch (itemUsed.itemName)
        {
            case "E_Tool":
            case "Food_Pack":
            case "Water_Canteen":
            case "ULF_Radio":
                itemUsed.UseItem(linkedIcon, null, null);
                break;
            case "Ammo_AR":
            case "Ammo_LMG":
            case "Ammo_Pi":
            case "Ammo_Ri":
            case "Ammo_Sh":
            case "Ammo_SMG":
            case "Ammo_Sn":
            case "Claymore":
                menu.OpenClaymoreUI(useItemUI);
                break;
            case "Deployment_Beacon":
                menu.OpenDeploymentBeaconUI(useItemUI);
                break;
            case "Thermal_Camera":
                menu.OpenThermalCamUI(useItemUI);
                break;
            case "Poison_Satchel":
                itemUsed.UseItem(linkedIcon, itemUsedOn, null);
                break;
            case "Grenade_Flashbang":
            case "Grenade_Frag":
            case "Grenade_Smoke":
            case "Grenade_Tabun":
                menu.OpenGrenadeUI(useItemUI);
                break;
            case "Medkit_Small":
            case "Medkit_Medium":
            case "Medkit_Large":
                itemUsed.UseItem(linkedIcon, null, soldierUsedOn);
                break;
            case "Riot_Shield":
                menu.OpenRiotShieldUI(useItemUI);
                break;
            case "Syringe_Amphetamine":
            case "Syringe_Androstenedione":
            case "Syringe_Cannabinoid":
            case "Syringe_Danazol":
            case "Syringe_Glucocorticoid":
            case "Syringe_Modafinil":
            case "Syringe_Shard":
            case "Syringe_Trenbolone":
            case "Syringe_Unlabelled":
                itemUsed.UseItem(linkedIcon, null, soldierUsedOn);
                break;
            case "UHF_Radio":
                menu.OpenUHFUI(useItemUI);
                break;
            default:
                break;

        }
        DeductAP(ap);
        menu.CloseUseItemUI();
    }
    public void UpdateSoldierUsedOn(UseItemUI useItemUI)
    {
        useItemUI.soldierUsedOn = soldierManager.FindSoldierById(menu.useItemUI.transform.Find("OptionPanel").Find("Target").Find("TargetDropdown").GetComponent<TMP_Dropdown>().options[menu.useItemUI.transform.Find("OptionPanel").Find("Target").Find("TargetDropdown").GetComponent<TMP_Dropdown>().value].text);
    }
    public void UpdateItemUsedOn(UseItemUI useItemUI)
    {
        useItemUI.itemUsedOn = itemManager.FindItemById(menu.useItemUI.transform.Find("OptionPanel").Find("Target").Find("TargetDropdown").GetComponent<TMP_Dropdown>().options[menu.useItemUI.transform.Find("OptionPanel").Find("Target").Find("TargetDropdown").GetComponent<TMP_Dropdown>().value].text);
    }
    public void ConfirmUHF(UseItemUI useUHFUI)
    {
        TMP_InputField targetX = useUHFUI.transform.Find("OptionPanel").Find("UHFTarget").Find("XPos").GetComponent<TMP_InputField>();
        TMP_InputField targetY = useUHFUI.transform.Find("OptionPanel").Find("UHFTarget").Find("YPos").GetComponent<TMP_InputField>();
        TMP_InputField targetZ = useUHFUI.transform.Find("OptionPanel").Find("UHFTarget").Find("ZPos").GetComponent<TMP_InputField>();
        TMP_Dropdown strikeOption = useUHFUI.transform.Find("OptionPanel").Find("StrikeOptions").Find("StrikeOptionsDropdown").GetComponent<TMP_Dropdown>();

        if (int.TryParse(strikeOption.options[strikeOption.value].text.Split('(', ')')[1], out int dipelecScore))
        {
            Tuple<int, string, int, int, int> strike = itemManager.GetStrike(dipelecScore);
            int rolls = strike.Item4;
            int radius = strike.Item3;
            int damage = strike.Item5;

            if (!useUHFUI.transform.Find("PressedOnce").gameObject.activeInHierarchy) //first press
            {
                
                if (targetX.textComponent.color == menu.normalTextColour && targetY.textComponent.color == menu.normalTextColour)
                {

                    int highestRoll = 0, newX, newY;
                    float scatterDistance;
                    int scatterDegree = RandomNumber(0, 360);
                    for (int i = 0; i < rolls; i++)
                    {
                        int roll = DiceRoll();
                        if (roll > highestRoll)
                            highestRoll = roll;
                    }

                    scatterDistance = highestRoll switch
                    {
                        2 => radius + 1,
                        3 => 0.75f * radius + 1,
                        4 => 0.5f * radius + 1,
                        5 => 0.25f * radius + 1,
                        6 => 0,
                        _ => -1,
                    };

                    if (scatterDistance != -1)
                    {
                        (newX, newY) = CalculateScatteredCoordinates(int.Parse(targetX.text), int.Parse(targetY.text), scatterDegree, scatterDistance);

                        if (newX > 0 && newX <= maxX && newY > 0 && newY <= maxY)
                        {
                            targetX.text = $"{newX}";
                            targetX.interactable = false;
                            targetY.text = $"{newY}";
                            targetY.interactable = false;
                            useUHFUI.transform.Find("OptionPanel").Find("UHFTarget").Find("ZLabel").gameObject.SetActive(true);
                            useUHFUI.transform.Find("OptionPanel").Find("UHFTarget").Find("ZPos").gameObject.SetActive(true);
                        }
                        else
                        {
                            useUHFUI.transform.Find("OptionPanel").Find("TotalMiss").Find("Text").GetComponent<TextMeshProUGUI>().text = "Scattered off map";
                            useUHFUI.transform.Find("OptionPanel").Find("TotalMiss").gameObject.SetActive(true);
                        }
                    }
                    else
                    {
                        useUHFUI.transform.Find("OptionPanel").Find("TotalMiss").Find("Text").GetComponent<TextMeshProUGUI>().text = "Total Missfire";
                        useUHFUI.transform.Find("OptionPanel").Find("TotalMiss").gameObject.SetActive(true);
                    }
                    useUHFUI.transform.Find("PressedOnce").gameObject.SetActive(true);
                }
            }
            else //second press
            {
                print("second press");

                if (useUHFUI.transform.Find("OptionPanel").Find("TotalMiss").gameObject.activeInHierarchy)
                {
                    menu.CloseUHFUI();
                    useUHFUI.itemUsed.UseItem(useUHFUI.itemUsedIcon, useUHFUI.itemUsedOn, useUHFUI.soldierUsedOn);
                }
                else
                {
                    if (float.TryParse(targetX.text, out float x) && float.TryParse(targetY.text, out float y) && float.TryParse(targetZ.text, out float z))
                    {
                        if (targetZ.textComponent.color == menu.normalTextColour)
                        {
                            if (useUHFUI.itemUsed.owner is Soldier linkedSoldier)
                            {
                                menu.CloseUHFUI();
                                useUHFUI.itemUsed.UseItem(useUHFUI.itemUsedIcon, useUHFUI.itemUsedOn, useUHFUI.soldierUsedOn);
                                CheckExplosionUHF(linkedSoldier, new Vector3(x, y, z), radius, damage);
                            }
                        }
                    }
                }
            }
        }
    }
    public void ConfirmRiotShield(UseItemUI useRiotShield)
    {
        TMP_InputField targetX = useRiotShield.transform.Find("OptionPanel").Find("RiotShieldTarget").Find("XPos").GetComponent<TMP_InputField>();
        TMP_InputField targetY = useRiotShield.transform.Find("OptionPanel").Find("RiotShieldTarget").Find("YPos").GetComponent<TMP_InputField>();

        if (targetX.textComponent.color == menu.normalTextColour && targetY.textComponent.color == menu.normalTextColour)
        {
            //set riot shield facing
            activeSoldier.riotXPoint = int.Parse(targetX.text);
            activeSoldier.riotYPoint = int.Parse(targetY.text);

            menu.CloseRiotShieldUI();
            useRiotShield.itemUsed.UseItem(useRiotShield.itemUsedIcon, useRiotShield.itemUsedOn, useRiotShield.soldierUsedOn);
        }
    }
    public void ConfirmGrenade(UseItemUI useGrenade)
    {
        string grenadeName = useGrenade.transform.Find("OptionPanel").Find("GrenadeName").Find("Text").GetComponent<TextMeshProUGUI>().text;
        TMP_InputField targetX = useGrenade.transform.Find("OptionPanel").Find("GrenadeTarget").Find("XPos").GetComponent<TMP_InputField>();
        TMP_InputField targetY = useGrenade.transform.Find("OptionPanel").Find("GrenadeTarget").Find("YPos").GetComponent<TMP_InputField>();
        TMP_InputField targetZ = useGrenade.transform.Find("OptionPanel").Find("GrenadeTarget").Find("ZPos").GetComponent<TMP_InputField>();
        if (!useGrenade.transform.Find("PressedOnce").gameObject.activeInHierarchy) //first press
        {
            if (targetX.textComponent.color == menu.normalTextColour && targetY.textComponent.color == menu.normalTextColour && targetZ.textComponent.color == menu.normalTextColour)
            {
                int newX, newY;
                int scatterDegree = RandomNumber(0, 360);
                int scatterDistance = activeSoldier.StrengthCheck() switch
                {
                    false => Mathf.CeilToInt(DiceRoll() * activeSoldier.stats.Str.Val / 2.0f),
                    _ => -1,
                };

                if (scatterDistance != -1)
                {
                    if (int.Parse(targetX.text) == activeSoldier.X && int.Parse(targetY.text) == activeSoldier.Y)
                        (newX, newY) = (int.Parse(targetX.text), int.Parse(targetY.text));
                    else
                        (newX, newY) = CalculateScatteredCoordinates(int.Parse(targetX.text), int.Parse(targetY.text), scatterDegree, scatterDistance);

                    targetX.text = $"{newX}";
                    targetY.text = $"{newY}";

                    if (newX <= 0 || newX > maxX || newY <= 0 || newY > maxY) //if scattering off map
                    {
                        useGrenade.transform.Find("OptionPanel").Find("TotalMiss").Find("Text").GetComponent<TextMeshProUGUI>().text = "Scattering off map";
                        useGrenade.transform.Find("OptionPanel").Find("TotalMiss").gameObject.SetActive(true);
                    }
                    useGrenade.transform.Find("OptionPanel").Find("GrenadeTarget").Find("FinalPosition").gameObject.SetActive(true);
                }
                else
                {
                    targetX.interactable = false;
                    targetY.interactable = false;
                    targetZ.interactable = false;
                    useGrenade.transform.Find("OptionPanel").Find("GrenadeTarget").Find("DirectHit").gameObject.SetActive(true);
                }

                useGrenade.transform.Find("PressedOnce").gameObject.SetActive(true);
            }
        }
        else //second press
        {
            (int newX, int newY) = (int.Parse(targetX.text), int.Parse(targetY.text));
            if (newX <= 0 || newX > maxX || newY <= 0 || newY > maxY)
            {
                useGrenade.itemUsed.UseItem(useGrenade.itemUsedIcon, useGrenade.itemUsedOn, useGrenade.soldierUsedOn);
                menu.CloseGrenadeUI();
            }
            else
            {
                if (targetX.textComponent.color == menu.normalTextColour && targetY.textComponent.color == menu.normalTextColour && targetZ.textComponent.color == menu.normalTextColour)
                {
                    if (useGrenade.itemUsed.owner is Soldier linkedSoldier)
                    {
                        useGrenade.itemUsed.UseItem(useGrenade.itemUsedIcon, useGrenade.itemUsedOn, useGrenade.soldierUsedOn);
                        if (grenadeName.Contains("Frag") || grenadeName.Contains("Flashbang"))
                            useGrenade.itemUsed.CheckExplosionGrenade(linkedSoldier, new Vector3(int.Parse(targetX.text), int.Parse(targetY.text), int.Parse(targetZ.text)));
                        else if (grenadeName.Contains("Smoke"))
                            Instantiate(poiManager.smokeCloudPrefab).Init(Tuple.Create(new Vector3(int.Parse(targetX.text), int.Parse(targetY.text), int.Parse(targetZ.text)), string.Empty), activeSoldier.Id);
                        else if (grenadeName.Contains("Tabun"))
                            Instantiate(poiManager.tabunCloudPrefab).Init(Tuple.Create(new Vector3(int.Parse(targetX.text), int.Parse(targetY.text), int.Parse(targetZ.text)), string.Empty), activeSoldier.Id);

                        menu.CloseGrenadeUI();
                    }
                }
            }
        }
    }
    public void ConfirmClaymore(UseItemUI useClaymore)
    {
        TMP_InputField placedX = useClaymore.transform.Find("OptionPanel").Find("ClaymorePlacing").Find("XPos").GetComponent<TMP_InputField>();
        TMP_InputField placedY = useClaymore.transform.Find("OptionPanel").Find("ClaymorePlacing").Find("YPos").GetComponent<TMP_InputField>();
        TMP_InputField placedZ = useClaymore.transform.Find("OptionPanel").Find("ClaymorePlacing").Find("ZPos").GetComponent<TMP_InputField>();
        TMP_Dropdown terrainOn = useClaymore.transform.Find("OptionPanel").Find("ClaymorePlacing").Find("Terrain").Find("TerrainDropdown").GetComponent<TMP_Dropdown>();
        TMP_InputField facingX = useClaymore.transform.Find("OptionPanel").Find("ClaymoreFacing").Find("XPos").GetComponent<TMP_InputField>();
        TMP_InputField facingY = useClaymore.transform.Find("OptionPanel").Find("ClaymoreFacing").Find("YPos").GetComponent<TMP_InputField>();
        

        if (placedX.textComponent.color == menu.normalTextColour && placedY.textComponent.color == menu.normalTextColour && placedZ.textComponent.color == menu.normalTextColour
        && facingX.textComponent.color == menu.normalTextColour && facingY.textComponent.color == menu.normalTextColour && terrainOn.value != 0)
        {
            if (int.TryParse(placedX.text, out int placedXInt) && int.TryParse(placedY.text, out int placedYInt) && int.TryParse(placedZ.text, out int placedZInt) 
            && int.TryParse(facingX.text, out int facingXInt) && int.TryParse(facingY.text, out int facingYInt))
            {
                if (CalculateRange(activeSoldier, new Vector3(placedXInt, placedYInt, placedZInt)) <= activeSoldier.SRColliderMin.radius)
                {
                    useClaymore.itemUsed.UseItem(useClaymore.itemUsedIcon, useClaymore.itemUsedOn, useClaymore.soldierUsedOn);
                    Instantiate(poiManager.claymorePrefab).Init(Tuple.Create(new Vector3(placedXInt, placedYInt, placedZInt), terrainOn.options[terrainOn.value].text), Tuple.Create(activeSoldier.stats.F.Val, activeSoldier.stats.C.Val, facingXInt, facingYInt, activeSoldier.Id)).PlaceClaymore();

                    menu.CloseClaymoreUI();
                }
                else
                    useClaymore.transform.Find("OptionPanel").Find("OutOfRange").gameObject.SetActive(true);
            }
        }
    }
    public void ConfirmDeploymentBeacon(UseItemUI useDeploymentBeacon)
    {
        TMP_InputField placedX = useDeploymentBeacon.transform.Find("OptionPanel").Find("BeaconPlacing").Find("XPos").GetComponent<TMP_InputField>();
        TMP_InputField placedY = useDeploymentBeacon.transform.Find("OptionPanel").Find("BeaconPlacing").Find("YPos").GetComponent<TMP_InputField>();
        TMP_InputField placedZ = useDeploymentBeacon.transform.Find("OptionPanel").Find("BeaconPlacing").Find("ZPos").GetComponent<TMP_InputField>();
        TMP_Dropdown terrainOn = useDeploymentBeacon.transform.Find("OptionPanel").Find("BeaconPlacing").Find("Terrain").Find("TerrainDropdown").GetComponent<TMP_Dropdown>();

        if (placedX.textComponent.color == menu.normalTextColour && placedY.textComponent.color == menu.normalTextColour && placedZ.textComponent.color == menu.normalTextColour && terrainOn.value != 0)
        {
            if (int.TryParse(placedX.text, out int placedXInt) && int.TryParse(placedY.text, out int placedYInt) && int.TryParse(placedZ.text, out int placedZInt))
            {
                if (CalculateRange(activeSoldier, new Vector3(placedXInt, placedYInt, placedZInt)) <= activeSoldier.SRColliderMin.radius)
                {
                    useDeploymentBeacon.itemUsed.UseItem(useDeploymentBeacon.itemUsedIcon, useDeploymentBeacon.itemUsedOn, useDeploymentBeacon.soldierUsedOn);
                    Instantiate(poiManager.deploymentBeaconPrefab).Init(Tuple.Create(new Vector3(placedXInt, placedYInt, placedZInt), terrainOn.options[terrainOn.value].text), activeSoldier.Id);

                    menu.CloseDeploymentBeaconUI();
                }
                else
                    useDeploymentBeacon.transform.Find("OptionPanel").Find("OutOfRange").gameObject.SetActive(true);
            }
        }
    }
    public void ConfirmThermalCam(UseItemUI useThermalCam)
    {
        TMP_InputField placedX = useThermalCam.transform.Find("OptionPanel").Find("CamPlacing").Find("XPos").GetComponent<TMP_InputField>();
        TMP_InputField placedY = useThermalCam.transform.Find("OptionPanel").Find("CamPlacing").Find("YPos").GetComponent<TMP_InputField>();
        TMP_InputField placedZ = useThermalCam.transform.Find("OptionPanel").Find("CamPlacing").Find("ZPos").GetComponent<TMP_InputField>();
        TMP_Dropdown terrainOn = useThermalCam.transform.Find("OptionPanel").Find("CamPlacing").Find("Terrain").Find("TerrainDropdown").GetComponent<TMP_Dropdown>();
        TMP_InputField facingX = useThermalCam.transform.Find("OptionPanel").Find("CamFacing").Find("XPos").GetComponent<TMP_InputField>();
        TMP_InputField facingY = useThermalCam.transform.Find("OptionPanel").Find("CamFacing").Find("YPos").GetComponent<TMP_InputField>();

        if (placedX.textComponent.color == menu.normalTextColour && placedY.textComponent.color == menu.normalTextColour && placedZ.textComponent.color == menu.normalTextColour
        && facingX.textComponent.color == menu.normalTextColour && facingY.textComponent.color == menu.normalTextColour && terrainOn.value != 0)
        {
            if (int.TryParse(placedX.text, out int placedXInt) && int.TryParse(placedY.text, out int placedYInt) && int.TryParse(placedZ.text, out int placedZInt)
            && int.TryParse(facingX.text, out int facingXInt) && int.TryParse(facingY.text, out int facingYInt))
            {
                if (CalculateRange(activeSoldier, new Vector3(placedXInt, placedYInt, placedZInt)) <= activeSoldier.SRColliderMin.radius)
                {
                    useThermalCam.itemUsed.UseItem(useThermalCam.itemUsedIcon, useThermalCam.itemUsedOn, useThermalCam.soldierUsedOn);
                    Instantiate(poiManager.thermalCamPrefab).Init(Tuple.Create(new Vector3(placedXInt, placedYInt, placedZInt), terrainOn.options[terrainOn.value].text), Tuple.Create(facingXInt, facingYInt, activeSoldier.Id));

                    menu.CloseThermalCamUI();
                }
                else
                    useThermalCam.transform.Find("OptionPanel").Find("OutOfRange").gameObject.SetActive(true);
            }
        }
    }
    static Tuple<int, int> CalculateScatteredCoordinates(int targetX, int targetY, float scatterDegree, float scatterDistance)
    {
        // Convert degree to radians
        double radians = Math.PI * scatterDegree / 180.0;

        // Calculate new coordinates
        int newX = Mathf.RoundToInt((float)(targetX + scatterDistance * Math.Cos(radians)));
        int newY = Mathf.RoundToInt((float)(targetY + scatterDistance * Math.Sin(radians)));

        return Tuple.Create(newX, newY);
    }
    public void CheckExplosionUHF(Soldier explodedBy, Vector3 position, int radius, int damage)
    {
        GameObject explosionList = Instantiate(menu.explosionListPrefab, menu.explosionUI.transform).GetComponent<ExplosionList>().Init($"UHF : {position.x}, {position.y}, {position.z}").gameObject;
        explosionList.transform.Find("ExplodedBy").GetComponent<TextMeshProUGUI>().text = explodedBy.id;

        foreach (PhysicalObject obj in FindObjectsOfType<PhysicalObject>())
        {
            float damagef = 0;
            if (obj.PhysicalObjectWithinRadius(position, radius / 2))
                damagef = damage;
            else if (obj.PhysicalObjectWithinRadius(position, radius))
                damagef = damage / 2.0f;

            if (damagef > 0)
            {
                if (obj is Item hitItem)
                    menu.AddExplosionAlertItem(explosionList, hitItem, position, explodedBy, Mathf.RoundToInt(damagef));
                else if (obj is POI hitPoi)
                    menu.AddExplosionAlertPOI(explosionList, hitPoi, explodedBy, Mathf.RoundToInt(damagef));
                else if (obj is Soldier hitSoldier)
                    menu.AddExplosionAlert(explosionList, hitSoldier, position, explodedBy, hitSoldier.RoundByResilience(damagef) - hitSoldier.stats.R.Val, 1);
            }
        }

        if (explosionList.transform.Find("Scroll").Find("View").Find("Content").childCount > 0)
            menu.OpenExplosionUI();
        else
            Destroy(explosionList);
    }
    public void CheckAllSmokeClouds()
    {
        foreach (Soldier s in AllSoldiers())
            if (!s.CheckSmokeClouds() && s.IsInSmoke())
                s.UnsetSmoked();
    }
    public void CheckAllTabunClouds()
    {
        foreach (Soldier s in AllSoldiers())
            if (!s.CheckTabunClouds() && s.IsInTabun())
                s.UnsetTabun();
    }
    public void IncreaseRoundsActiveAllClouds()
    {
        foreach (SmokeCloud cloud in FindObjectsOfType<SmokeCloud>())
            cloud.TurnsUntilDissipation--;
        foreach (TabunCloud cloud in FindObjectsOfType<TabunCloud>())
            cloud.TurnsUntilDissipation--;
    }






    //dipelec functions
    public void UpdateDipElecUI()
    {
        if (!menu.clearDipelecFlag)
            UpdateDipElecRewardAndChance();

    }
    public void UpdateDipElecRewardAndChance()
    {
        TMP_Dropdown dipelecType = menu.dipelecUI.transform.Find("DipElecType").Find("DipElecTypeDropdown").GetComponent<TMP_Dropdown>();
        TMP_Dropdown level = menu.dipelecUI.transform.Find("Level").Find("LevelDropdown").GetComponent<TMP_Dropdown>();
        level.GetComponent<DropdownController>().optionsToGrey.Clear();

        if (dipelecType.value == 0)
        {
            menu.dipelecUI.transform.Find("Level").gameObject.SetActive(true);
            for (int i = 1; i <= 6; i++)
                if (activeSoldier.stats.Dip.Val + activeSoldier.TacticianBonus() < i)
                    level.GetComponent<DropdownController>().optionsToGrey.Add($"{i}");
            if (level.GetComponent<DropdownController>().optionsToGrey.Contains($"{level.value + 1}"))
                level.value = 0;

            menu.dipelecUI.transform.Find("Reward").Find("Reward").Find("RewardDisplay").GetComponent<TextMeshProUGUI>().text = dipelec.GetLevelDip(level.value + 1);
            menu.dipelecUI.transform.Find("SuccessChance").Find("SuccessChanceDisplay").GetComponent<TextMeshProUGUI>().text = Mathf.FloorToInt(CumulativeBinomialProbability(activeSoldier.stats.Dip.Val + activeSoldier.TacticianBonus(), level.value + 1, 0.5f, 0.5f) * 100f).ToString() + "%";
        }
        else if (dipelecType.value == 1)
        {
            menu.dipelecUI.transform.Find("Level").gameObject.SetActive(true);
            for (int i = 1; i <= 6; i++)
                if (activeSoldier.stats.Elec.Val + activeSoldier.CalculatorBonus() < i)
                    level.GetComponent<DropdownController>().optionsToGrey.Add($"{i}");
            if (level.GetComponent<DropdownController>().optionsToGrey.Contains($"{level.value + 1}"))
                level.value = 0;
            menu.dipelecUI.transform.Find("Reward").Find("Reward").Find("RewardDisplay").GetComponent<TextMeshProUGUI>().text = dipelec.GetLevelElec(level.value + 1);
            menu.dipelecUI.transform.Find("SuccessChance").Find("SuccessChanceDisplay").GetComponent<TextMeshProUGUI>().text = Mathf.FloorToInt(CumulativeBinomialProbability(activeSoldier.stats.Elec.Val + activeSoldier.CalculatorBonus(), level.value + 1, 0.5f, 0.5f) * 100f).ToString() + "%";
        }
        else
        {
            menu.dipelecUI.transform.Find("Level").gameObject.SetActive(false);
            menu.dipelecUI.transform.Find("Reward").Find("Reward").Find("RewardDisplay").GetComponent<TextMeshProUGUI>().text = "Permanently disable this terminal?\n<color=red>Warning: Penalties Apply</color>";
            menu.dipelecUI.transform.Find("SuccessChance").Find("SuccessChanceDisplay").GetComponent<TextMeshProUGUI>().text = "100%";
        }
    }
    public void ConfirmDipElec()
    {
        if (CheckAP(3))
        {
            DeductAP(3);
            bool terminalDisabled = false;
            int passCount = 0;
            string resultString = "";
            TMP_Dropdown levelDropdown = menu.dipelecUI.transform.Find("Level").Find("LevelDropdown").GetComponent<TMP_Dropdown>();
            Terminal terminal = poiManager.FindPOIById(menu.dipelecUI.transform.Find("Terminal").GetComponent<TextMeshProUGUI>().text) as Terminal;

            if (menu.dipelecUI.transform.Find("DipElecType").Find("DipElecTypeDropdown").GetComponent<TMP_Dropdown>().value == 0)
            {
                terminal.SoldiersAlreadyNegotiated.Add(activeSoldier.id);
                for (int i = 0; i < activeSoldier.stats.Dip.Val; i++)
                {
                    if (CoinFlip())
                        passCount++;
                }
                resultString = "Negotiation";
            }
            else if (menu.dipelecUI.transform.Find("DipElecType").Find("DipElecTypeDropdown").GetComponent<TMP_Dropdown>().value == 1)
            {
                terminal.SoldiersAlreadyHacked.Add(activeSoldier.id);
                for (int i = 0; i < activeSoldier.stats.Elec.Val; i++)
                {
                    if (CoinFlip())
                        passCount++;
                }
                resultString = "Hack";
            }
            else
            {
                terminalDisabled = true;
            }

            if (!terminalDisabled)
            {
                if (passCount > levelDropdown.value)
                {
                    menu.dipelecResultUI.transform.Find("OptionPanel").Find("Result").Find("ResultDisplay").GetComponent<TextMeshProUGUI>().text = $"<color=green>Successful {resultString}</color>";
                    menu.AddXpAlert(activeSoldier, (int)Mathf.Pow(2, levelDropdown.value), $"Successful level {levelDropdown.value + 1} {resultString}", true);
                    menu.dipelecUI.transform.Find("Level").gameObject.SetActive(true);
                    menu.dipelecResultUI.transform.Find("RewardPanel").gameObject.SetActive(true);
                    for (int i = 0; i <= levelDropdown.value; i++)
                    {
                        print("got into loop");
                        GameObject dipelecReward = Instantiate(menu.dipelecRewardPrefab, menu.dipelecResultUI.transform.Find("RewardPanel").Find("Scroll").Find("View").Find("Content"));
                        print("passed instantiation");
                        TextMeshProUGUI textComponent = dipelecReward.GetComponentInChildren<TextMeshProUGUI>();

                        textComponent.text = (resultString == "Hack") ? dipelec.GetLevelElec(i+1) : dipelec.GetLevelDip(i+1);
                    }
                }
                else
                {
                    menu.dipelecResultUI.transform.Find("OptionPanel").Find("Result").Find("ResultDisplay").GetComponent<TextMeshProUGUI>().text = "Failed " + resultString;
                    menu.dipelecResultUI.transform.Find("RewardPanel").gameObject.SetActive(false);
                }
            }
            else
            {
                menu.dipelecResultUI.transform.Find("RewardPanel").gameObject.SetActive(false);
                menu.dipelecResultUI.transform.Find("OptionPanel").Find("Result").Find("ResultDisplay").GetComponent<TextMeshProUGUI>().text = "Terminal Disabled!";
                terminal.terminalEnabled = false;
                if (activeSoldier.hp > 3)
                    activeSoldier.TakeDamage(activeSoldier, activeSoldier.hp - 3, true, new() { "Dipelec" });
            }

            menu.OpenDipelecResultUI();
            menu.CloseDipElecUI();
        }
    }
















    //AP/MP functions
    public bool CheckAP(int ap)
    {
        //check if it's on the current players turn
        if (menu.overrideView)
            return true;
        else
        {
            if (activeSoldier.ap >= ap)
                return true;
            else
            {
                notEnoughAPUI.SetActive(true);
                return false;
            }
        }
    }
    public bool CheckMP(int mp)
    {
        if (menu.overrideView)
            return true;
        else
        {
            if (activeSoldier.mp >= mp)
                return true;
            else
            {
                notEnoughMPUI.SetActive(true);
                return false;
            }
        }
    }
    public void DeductAP(int ap)
    {
        if (!menu.overrideView && ap > 0)
        {
            activeSoldier.ap -= ap;
            activeSoldier.usedAP = true;

            //break spotter ability
            activeSoldier.RemoveAllSpotting();
        }
    }
    public void DeductMP(int mp)
    {
        if (!menu.overrideView && mp > 0)
        {
            activeSoldier.mp -= mp;
            activeSoldier.usedMP = true;
        }
    }
    public void DrainAP()
    {
        activeSoldier.ap = 0;
        activeSoldier.usedAP = true;
    }
    public void DrainMP()
    {
        activeSoldier.mp = 0;
        activeSoldier.usedMP = true;
    }

    //lastandicide functions
    public void Lastandicide()
    {
        if (CheckAP(1))
        {
            DeductAP(1);
            activeSoldier.InstantKill(activeSoldier, new List<string> { "Lastandicide" });
        }
    }

    public void CloseNotEnoughAP()
    {
        notEnoughAPUI.SetActive(false);
    }
    public void CloseNotEnoughMP()
    {
        notEnoughMPUI.SetActive(false);
    }

    //trauma functions
    public IEnumerator TraumaCheck(Soldier deadSoldier, int tp, bool commander, bool lastandicide)
    {
        //print("Trauma check start");
        //imperceptible delay to allow colliders to be recalculated at new destination
        yield return new WaitForSeconds(0.05f);
        //print("Trauma check passed wait");
        if (deadSoldier.IsDead())
        {
            int numberOfPasses;
            bool showTraumaUI = false, commanderPassFinished = false, lastandicidePassFinished = false;

            //check how many times to loop through checks
            if (commander && lastandicide)
                numberOfPasses = 3;
            else if (commander || lastandicide)
                numberOfPasses = 2;
            else
                numberOfPasses = 1;

            for (int i = 0; i < numberOfPasses; i++)
            {
                foreach (Soldier friendly in AllSoldiers())
                {
                    //print(friendly.soldierName + " trauma check attempting to run");

                    if (friendly.IsSameTeamAs(deadSoldier))
                    {
                        //print(friendly.soldierName + " trauma check actually running");
                        //desensitised
                        if (friendly.IsDesensitised())
                            menu.AddTraumaAlert(friendly, tp, friendly.soldierName + " is " + friendly.GetTraumaState() + ". He is immune to trauma.", 0, 0, "");
                        else
                        {
                            //guaranteed trauma from commander death and/or lastandicide
                            if (commander)
                            {
                                menu.AddTraumaAlert(friendly, 1, "Commander died, an automatic trauma point has been accrued.", 0, 0, "");
                                commanderPassFinished = true;
                                showTraumaUI = true;
                            }
                            else if (lastandicide)
                            {
                                menu.AddTraumaAlert(friendly, 1, deadSoldier.soldierName + " committed Lastandicide, an automatic trauma point has been accrued.", 0, 0, "");
                                lastandicidePassFinished = true;
                                showTraumaUI = true;
                            }
                            else
                            {
                                //check friendlies seeing death
                                if (friendly.IsAbleToSee())
                                {
                                    int rolls = 0, xpOnResist = 0;
                                    if (friendly.PhysicalObjectWithinMaxRadius(deadSoldier))
                                    {
                                        string range = CalculateRangeBracket(CalculateRange(deadSoldier, friendly));
                                        switch (range)
                                        {
                                            case "Melee":
                                                rolls = 0;
                                                xpOnResist = 5;
                                                break;
                                            case "CQB":
                                                rolls = 0;
                                                xpOnResist = 4;
                                                break;
                                            case "Short":
                                                rolls = 1;
                                                xpOnResist = 3;
                                                break;
                                            case "Medium":
                                                rolls = 2;
                                                xpOnResist = 2;
                                                break;
                                            case "Long":
                                                rolls = 3;
                                                xpOnResist = 1;
                                                break;
                                            default:
                                                rolls = 3;
                                                xpOnResist = 1;
                                                break;
                                        }

                                        //add rolls for rank differential
                                        if (deadSoldier.MinXPForRank() > friendly.MinXPForRank())
                                            rolls += 0;
                                        else if (deadSoldier.MinXPForRank() == friendly.MinXPForRank())
                                            rolls += 1;
                                        else
                                            rolls += 2;

                                        if (friendly.IsResilient())
                                            menu.AddTraumaAlert(friendly, tp, $"{friendly.soldierName} is Resilient. Within {range} range of {deadSoldier.soldierName}. Check for LOS?", rolls, xpOnResist, range);
                                        else
                                            menu.AddTraumaAlert(friendly, tp, $"{friendly.soldierName} is within {range} range of {deadSoldier.soldierName}. Check for LOS?", rolls, xpOnResist, range);

                                        showTraumaUI = true;
                                    }
                                }
                            }
                        }
                    }
                }

                if (commanderPassFinished)
                    commander = false;
                if (lastandicidePassFinished)
                    lastandicide = false;
            }

            if (showTraumaUI)
            {
                yield return new WaitUntil(() => menu.meleeResolvedFlag == true);
                menu.OpenTraumaAlertUI();
            }
        }
    }
    public void ConfirmTrauma()
    {
        ScrollRect traumaScroller = menu.traumaUI.transform.Find("OptionPanel").Find("Scroll").GetComponent<ScrollRect>();
        bool unresolved = false;

        if (traumaScroller.verticalNormalizedPosition <= 0.05f)
        {
            Transform traumaAlerts = menu.traumaUI.transform.Find("OptionPanel").Find("Scroll").Find("View").Find("Content");

            foreach (Transform child in traumaAlerts)
            {
                if (child.Find("TraumaGainTitle").GetComponent<TextMeshProUGUI>().text.Contains("POTENTIAL"))
                    unresolved = true;
            }

            //block confirm if all buttons haven't been clicked
            if (!unresolved)
            {
                Dictionary<Soldier, int> soldiersToTraumatise = new();
                foreach (Transform child in traumaAlerts)
                {
                    if (child.Find("TraumaGainTitle").GetComponent<TextMeshProUGUI>().text.Contains("GAINED"))
                    {
                        Soldier traumatisedSoldier = child.GetComponent<SoldierAlert>().soldier;
                        int.TryParse(child .Find("TraumaIndicator").GetComponent<TextMeshProUGUI>().text, out int trauma);

                        // Check if the soldier is already in the dictionary
                        if (soldiersToTraumatise.ContainsKey(traumatisedSoldier))
                            soldiersToTraumatise[traumatisedSoldier] += trauma;
                        else
                            soldiersToTraumatise.Add(traumatisedSoldier, trauma);
                    }
                    Destroy(child.gameObject);
                }

                //iterate through the list 
                foreach (KeyValuePair<Soldier, int> kvp in soldiersToTraumatise)
                    kvp.Key.TakeTrauma(kvp.Value);

                menu.CloseTraumaUI();
            }
            else
                print("Haven't traumatised everyone.");
        }
        else
            print("Haven't scrolled all the way to the bottom");
    }














    //damage event functions - game
    public void ConfirmDamageEvent()
    {
        if (damageEventTypeDropdown.options[damageEventTypeDropdown.value].text.Contains("Bloodletting"))
        {
            activeSoldier.TakeBloodlettingDamage();
            menu.CloseDamageEventUI();
        }
        else if (damageEventTypeDropdown.options[damageEventTypeDropdown.value].text.Contains("Other") && int.TryParse(menu.damageEventUI.transform.Find("Other").Find("OtherInput").GetComponent<TMP_InputField>().text, out int otherDamage))
        {
            activeSoldier.TakeDamage(null, otherDamage, false, new() { menu.damageEventUI.transform.Find("DamageSource").Find("DamageSourceInput").GetComponent<TMP_InputField>().text });
            menu.CloseDamageEventUI();
        }
        else
        {
            //check input
            if (GetFallOrCollapseLocation(out Tuple<Vector3, string> fallCollapseLocation))
            {
                if (damageEventTypeDropdown.options[damageEventTypeDropdown.value].text.Contains("Fall"))
                    activeSoldier.TakeDamage(null, CalculateFallDamage(activeSoldier, int.Parse(menu.damageEventUI.transform.Find("FallDistance").Find("FallInputZ").GetComponent<TMP_InputField>().text)), false, new() { "Fall" });
                else if (damageEventTypeDropdown.options[damageEventTypeDropdown.value].text.Contains("Collapse"))
                {
                    int structureHeight = int.Parse(menu.damageEventUI.transform.Find("StructureHeight").Find("StructureHeightInputZ").GetComponent<TMP_InputField>().text);
                    //add xp if survives, otherwise kill
                    if (activeSoldier.StructuralCollapseCheck(structureHeight))
                    {
                        menu.AddXpAlert(activeSoldier, activeSoldier.stats.R.Val, $"Survived a {structureHeight}cm structural collapse.", true);
                        menu.AddDamageAlert(activeSoldier, $"{activeSoldier.soldierName} survived a {structureHeight}cm structural collapse.", true, false);
                    }
                    else
                    {
                        if (activeSoldier.IsWearingJuggernautArmour(false))
                        {
                            activeSoldier.MakeUnconscious();
                            menu.AddDamageAlert(activeSoldier, $"{activeSoldier.soldierName} survived a {structureHeight}cm structural collapse with Juggernaut Armour.", true, false);
                        }
                        else
                        {
                            activeSoldier.InstantKill(null, new List<string>() { "Structural Collapse" });
                            activeSoldier.SetCrushed();
                        }
                    }
                }

                //move actually proceeds
                PerformMove(activeSoldier, 0, fallCollapseLocation, false, false, string.Empty, true);

                menu.CloseDamageEventUI();

            }
            else
                print("Invalid Input");
        }
    }
    public bool GetFallOrCollapseLocation(out Tuple<Vector3, string> fallCollapseLocation)
    {
        fallCollapseLocation = default;
        if (menu.damageEventUI.transform.Find("Location").Find("XPos").GetComponent<TMP_InputField>().textComponent.GetComponent<TextMeshProUGUI>().color == menu.normalTextColour &&
            menu.damageEventUI.transform.Find("Location").Find("YPos").GetComponent<TMP_InputField>().textComponent.GetComponent<TextMeshProUGUI>().color == menu.normalTextColour &&
            menu.damageEventUI.transform.Find("Location").Find("ZPos").GetComponent<TMP_InputField>().textComponent.GetComponent<TextMeshProUGUI>().color == menu.normalTextColour &&
            menu.damageEventUI.transform.Find("Location").Find("Terrain").Find("TerrainDropdown").GetComponent<TMP_Dropdown>().value != 0)
        {
            fallCollapseLocation = Tuple.Create(new Vector3(int.Parse(menu.damageEventUI.transform.Find("Location").Find("XPos").GetComponent<TMP_InputField>().text),
                int.Parse(menu.damageEventUI.transform.Find("Location").Find("YPos").GetComponent<TMP_InputField>().text),
                int.Parse(menu.damageEventUI.transform.Find("Location").Find("ZPos").GetComponent<TMP_InputField>().text)), 
                menu.damageEventUI.transform.Find("Location").Find("Terrain").Find("TerrainDropdown").GetComponent<TMP_Dropdown>().options[menu.damageEventUI.transform.Find("Location").Find("Terrain").Find("TerrainDropdown").GetComponent<TMP_Dropdown>().value].text);

            return true;
        }

        return false;
    }






    //inspirer functions
    public void CheckInspirer(Soldier inspirer)
    {
        menu.SetInspirerResolvedFlagTo(false);
        bool openInspirerUI = false;

        foreach (Soldier friendly in AllSoldiers())
        {
            if (friendly.IsAbleToSee() && inspirer.IsSameTeamAs(friendly) && friendly.PhysicalObjectWithinMaxRadius(inspirer) && !friendly.IsRevoker())
            {
                menu.AddInspirerAlert(friendly, "An Inspirer (" + inspirer.soldierName + ") is within SR range of " + friendly.soldierName + ". Is LOS present?");
                openInspirerUI = true; 
            }
        }

        if (openInspirerUI)
            menu.OpenInspirerUI();
        else
            menu.SetInspirerResolvedFlagTo(true);
    }
    public void ConfirmInspirer()
    {
        ScrollRect inspireScroller = menu.inspirerUI.transform.Find("OptionPanel").Find("Scroll").GetComponent<ScrollRect>();

        if (inspireScroller.verticalNormalizedPosition <= 0.05f)
        {
            Transform inspirerAlerts = menu.inspirerUI.transform.Find("OptionPanel").Find("Scroll").Find("View").Find("Content");

            foreach (Transform child in inspirerAlerts)
            {
                if (child.Find("InspirerToggle").GetComponent<Toggle>().isOn)
                    child.GetComponent<SoldierAlert>().soldier.SetInspired();
            }

            //destroy inspirer alerts
            foreach (Transform child in inspirerAlerts)
                Destroy(child.gameObject);

            menu.CloseInspirerUI();
        }
        else
            print("Haven't scrolled all the way to the bottom");
    }




    //xp functions - game
    public int CalculateShotKillXp(Soldier killer, Soldier deadMan)
    {
        int xp = 0;
        if (killer.IsOppositeTeamAs(deadMan)) //only pay xp if killing opponent
        {
            int rankDifference = killer.RankDifferenceTo(deadMan);

            if (killer.IsHigherRankThan(deadMan))
                xp = 6 + Mathf.CeilToInt(Mathf.Pow(rankDifference, 2) / 2);
            else
                xp = 6 + rankDifference;

            if (xp < 0)
                xp = 0;
        }
        return xp;
    }
    public int CalculateMeleeKillXp(Soldier killer, Soldier deadMan)
    {
        int xp = 0;
        if (killer.IsOppositeTeamAs(deadMan)) //only pay xp if killing opponent
        {
            int rankDifference = killer.RankDifferenceTo(deadMan);

            if (killer.IsHigherRankThan(deadMan))
                xp = 10 + Mathf.CeilToInt(Mathf.Pow(rankDifference, 2) / 2);
            else
                xp = 10 + rankDifference;

            if (xp < 0)
                xp = 0;
        }
        return xp;
    }
    public int CalculateMeleeCounterKillXp(Soldier killer, Soldier deadMan)
    {
        int xp = 0;
        if (killer.IsOppositeTeamAs(deadMan)) //only pay xp if killing opponent
        {
            int rankDifference = killer.RankDifferenceTo(deadMan);

            if (killer.IsHigherRankThan(deadMan))
                xp = 20 + Mathf.CeilToInt(Mathf.Pow(rankDifference, 2) / 2);
            else
                xp = 20 + rankDifference;

            if (xp < 0)
                xp = 0;
        }
        return xp;
    }





    //detection functions
    public float CalculateRange(PhysicalObject obj1, PhysicalObject obj2)
    {
        return Vector3.Distance(new Vector3(obj1.X, obj1.Y, obj1.Z), new Vector3(obj2.X, obj2.Y, obj2.Z));
    }
    public float CalculateRange(PhysicalObject s1, Vector3 point)
    {
        return Vector3.Distance(new Vector3(s1.X, s1.Y, s1.Z), point);
    }
    public string CalculateRangeBracket(float range)
    {
        if (range <= 3)
            return "Melee";
        else if (range <= 10)
            return "CQB";
        else if (range <= 30)
            return "Short";
        else if (range <= 70)
            return "Medium";
        else if (range <= 150)
            return "Long";
        else
            return "Coriolis";
    }
    public bool GlimpseDetectionDetecteeSeesMovingThroughOverwatchCone(Soldier movingSoldier, Soldier detectee, Vector3 movingSoldierOldPosition)
    {
        //reset bound crosses to zero
        boundCrossOne = Vector3.zero;
        boundCrossTwo = Vector3.zero;

        List<Vector3> previousPositionsMovingSoldier = new();
        List<bool> detecteeSeesMovingSoldier = new();
        int boundCrossCount = 0;

        float maxSteps = CalculateRange(movingSoldier, movingSoldierOldPosition);

        //print("Radius:" + detectee.SRColliderMax.radius);
        for (int i = 0; i < maxSteps; i++)
        {
            Vector3 position = new(movingSoldierOldPosition.x + (movingSoldier.X - movingSoldierOldPosition.x) * (i / maxSteps), movingSoldierOldPosition.y + (movingSoldier.Y - movingSoldierOldPosition.y) * (i / maxSteps), movingSoldierOldPosition.z + (movingSoldier.Z - movingSoldierOldPosition.z) * (i / maxSteps));
            previousPositionsMovingSoldier.Add(position);

            //record when detectee sees moving soldier
            if (detectee.PhysicalObjectWithinOverwatchCone(position))
                detecteeSeesMovingSoldier.Add(true);
            else
                detecteeSeesMovingSoldier.Add(false);

            //print("Point " + i + ": " + CalculateRange(detectee, position));
        }

        //find borders where moving soldier crossed into detectee radius
        for (int i = 0; i < detecteeSeesMovingSoldier.Count - 1; i++)
        {
            if (detecteeSeesMovingSoldier[i] != detecteeSeesMovingSoldier[i + 1])
            {
                boundCrossCount++;
                if (boundCrossOne == Vector3.zero)
                    boundCrossOne = new(Mathf.Round(previousPositionsMovingSoldier[i].x), Mathf.Round(previousPositionsMovingSoldier[i].y), Mathf.Round(previousPositionsMovingSoldier[i].z));
                else
                    boundCrossTwo = new(Mathf.Round(previousPositionsMovingSoldier[i].x), Mathf.Round(previousPositionsMovingSoldier[i].y), Mathf.Round(previousPositionsMovingSoldier[i].z));
            }
        }

        print("DetecteeSeesMoving: Bound cross count: " + boundCrossCount);
        print("DetecteeSeesMoving: Bound cross one = " + "X:" + boundCrossOne.x + " Y:" + boundCrossOne.y + " Z:" + boundCrossOne.z);
        print("DetecteeSeesMoving: Bound cross two = " + "X:" + boundCrossTwo.x + " Y:" + boundCrossTwo.y + " Z:" + boundCrossTwo.z);

        if (boundCrossCount > 0)
            return true;

        return false;
    }
    public bool GlimpseDetectionDetecteeSeesMoving(Soldier movingSoldier, Soldier detectee, Vector3 movingSoldierOldPosition)
    {
        //reset bound crosses to zero
        boundCrossOne = Vector3.zero;
        boundCrossTwo = Vector3.zero;

        List<Vector3> previousPositionsMovingSoldier = new();
        List<bool> detecteeSeesMovingSoldier = new();
        int boundCrossCount = 0;

        float maxSteps = CalculateRange(movingSoldier, movingSoldierOldPosition);

        //print("Radius:" + detectee.SRColliderMax.radius);
        for (int i = 0; i < maxSteps; i++)
        {
            Vector3 position = new(movingSoldierOldPosition.x + (movingSoldier.X - movingSoldierOldPosition.x) * (i / maxSteps), movingSoldierOldPosition.y + (movingSoldier.Y - movingSoldierOldPosition.y) * (i / maxSteps), movingSoldierOldPosition.z + (movingSoldier.Z - movingSoldierOldPosition.z) * (i / maxSteps));
            previousPositionsMovingSoldier.Add(position);

            //record when detectee sees moving soldier
            if (CalculateRange(detectee, position) <= detectee.SRColliderMax.radius)
                detecteeSeesMovingSoldier.Add(true);
            else
                detecteeSeesMovingSoldier.Add(false);

            //print("Point " + i + ": " + CalculateRangeCover(detectee, position));
        }

        //find borders where moving soldier crossed into detectee radius
        for (int i = 0; i < detecteeSeesMovingSoldier.Count - 1; i++)
        {
            if (detecteeSeesMovingSoldier[i] != detecteeSeesMovingSoldier[i + 1])
            {
                boundCrossCount++;
                if (boundCrossOne == Vector3.zero)
                    boundCrossOne = new(Mathf.Round(previousPositionsMovingSoldier[i].x), Mathf.Round(previousPositionsMovingSoldier[i].y), Mathf.Round(previousPositionsMovingSoldier[i].z));
                else
                    boundCrossTwo = new(Mathf.Round(previousPositionsMovingSoldier[i].x), Mathf.Round(previousPositionsMovingSoldier[i].y), Mathf.Round(previousPositionsMovingSoldier[i].z));
            }
        }

        print("DetecteeSeesMoving: Bound cross count: " + boundCrossCount);
        print("DetecteeSeesMoving: Bound cross one = " + "X:" + boundCrossOne.x + " Y:" + boundCrossOne.y + " Z:" + boundCrossOne.z);
        print("DetecteeSeesMoving: Bound cross two = " + "X:" + boundCrossTwo.x + " Y:" + boundCrossTwo.y + " Z:" + boundCrossTwo.z);

        if (boundCrossCount > 0)
            return true;
        else
            return false;
    }

    public bool GlimpseDetectionMovingSeesDetectee(Soldier movingSoldier, Soldier detectee, Vector3 movingSoldierOldPosition)
    {
        //reset bound crosses to zero
        boundCrossOne = Vector3.zero;
        boundCrossTwo = Vector3.zero;

        List<Vector3> previousPositionsMovingSoldier = new();
        List<bool> movingSoldierSeesDetectee = new();
        int boundCrossCount = 0;

        float maxSteps = CalculateRange(movingSoldier, movingSoldierOldPosition);

        for (int i = 0; i < maxSteps; i++)
        {
            Vector3 position = new(movingSoldierOldPosition.x + (movingSoldier.X - movingSoldierOldPosition.x) * (i / maxSteps), movingSoldierOldPosition.y + (movingSoldier.Y - movingSoldierOldPosition.y) * (i / maxSteps), movingSoldierOldPosition.z + (movingSoldier.Z - movingSoldierOldPosition.z) * (i / maxSteps));
            previousPositionsMovingSoldier.Add(position);

            //record when moving solider sees detectee
            if (CalculateRange(detectee, position) <= movingSoldier.SRColliderMax.radius)
                movingSoldierSeesDetectee.Add(true);
            else
                movingSoldierSeesDetectee.Add(false);
        }

        //find borders where moving soldier radius crossed over detectee
        for (int i = 0; i < movingSoldierSeesDetectee.Count - 1; i++)
        {
            if (movingSoldierSeesDetectee[i] != movingSoldierSeesDetectee[i + 1])
            {
                boundCrossCount++;
                if (boundCrossOne == Vector3.zero)
                    boundCrossOne = new(Mathf.Round(previousPositionsMovingSoldier[i].x), Mathf.Round(previousPositionsMovingSoldier[i].y), Mathf.Round(previousPositionsMovingSoldier[i].z));
                else
                    boundCrossTwo = new(Mathf.Round(previousPositionsMovingSoldier[i].x), Mathf.Round(previousPositionsMovingSoldier[i].y), Mathf.Round(previousPositionsMovingSoldier[i].z));
            }
        }

        print("MovingSeesDetectee: Bound cross count: " + boundCrossCount);
        print("MovingSeesDetectee: Bound cross one = " + "X:" + boundCrossOne.x + " Y:" + boundCrossOne.y + " Z:" + boundCrossOne.z);
        print("MovingSeesDetectee: Bound cross two = " + "X:" + boundCrossTwo.x + " Y:" + boundCrossTwo.y + " Z:" + boundCrossTwo.z);

        if (boundCrossCount > 0)
            return true;
        else
            return false;
    }

    public int CalculateMoveDistance(Vector3 moveToLocation)
    {
        return Mathf.RoundToInt(Vector3.Distance(new Vector3(activeSoldier.X, activeSoldier.Y, activeSoldier.Z), moveToLocation));
    }
    public IEnumerator DetectionAlertSingle(Soldier movingSoldier, string causeOfLosCheck, Vector3 movingSoldierOldPosition, string launchMelee, bool triggersOverwatch)
    {
        yield return new WaitUntil(() => menu.shotResolvedFlag == true && menu.meleeResolvedFlag == true && menu.inspirerResolvedFlag == true && menu.overrideView == false);
        string movingSoldierActiveStat = "F";
        string detecteeActiveStat = "C";
        int[] movingSoldierMultipliers = { 3, 2, 1 };
        int[] detecteeMultipliers = { 4, 3, 2 };

        //if moving soldier is offturn, swap the default active stats and Perceptiveness multipliers
        if (movingSoldier.IsOffturnAndAlive())
        {
            (detecteeActiveStat, movingSoldierActiveStat) = (movingSoldierActiveStat, detecteeActiveStat);
            (detecteeMultipliers, movingSoldierMultipliers) = (movingSoldierMultipliers, detecteeMultipliers);
        }

        //imperceptible delay to allow colliders to be recalculated at new destination
        yield return new WaitForSeconds(0.05f);

        if (movingSoldier.IsAlive())
        {
            bool showDetectionUI = false;
            foreach (IAmDetectable detectee in AllDetectable())
            {
                bool detectedLeft = false, detectedRight = false, avoidanceLeft = false, avoidanceRight = false, noDetectRight = false, noDetectLeft = false, overwatchRight = false, overwatchLeft = false;
                string arrowType = "";
                string detectorLabel = "", counterLabel = "";

                if (detectee is Soldier detecteeSoldier && detecteeSoldier.IsAlive() && movingSoldier.IsOppositeTeamAs(detecteeSoldier))
                {
                    bool addDetection = false;

                    //check moving soldier seeing static soldiers
                    if (movingSoldier.IsAbleToSee())
                    {
                        if (movingSoldier.PhysicalObjectWithinMinRadius(detecteeSoldier))
                        {
                            addDetection = true;
                            
                            if (detecteeSoldier.AvoidanceActiveStat(detecteeActiveStat) > movingSoldier.DetectionActiveStat(movingSoldierMultipliers[0]))
                            {
                                detectorLabel += CreateDetectionMessage("avoidance", "3cm", detecteeActiveStat, detecteeSoldier.AvoidanceActiveStat(detecteeActiveStat), movingSoldierMultipliers[0], movingSoldier.stats.P.Val);
                                avoidanceRight = true;
                            }
                            else
                            {
                                detectorLabel += CreateDetectionMessage("detection", "3cm", detecteeActiveStat, detecteeSoldier.AvoidanceActiveStat(detecteeActiveStat), movingSoldierMultipliers[0], movingSoldier.stats.P.Val);
                                detectedRight = true;
                            }
                        }
                        else if (movingSoldier.PhysicalObjectWithinHalfRadius(detecteeSoldier))
                        {
                            addDetection = true;

                            if (detecteeSoldier.AvoidanceActiveStat(detecteeActiveStat) > movingSoldier.DetectionActiveStat(movingSoldierMultipliers[1]))
                            {
                                detectorLabel += CreateDetectionMessage("avoidance", "half SR", detecteeActiveStat, detecteeSoldier.AvoidanceActiveStat(detecteeActiveStat), movingSoldierMultipliers[1], movingSoldier.stats.P.Val);
                                avoidanceRight = true;
                            }
                            else
                            {
                                detectorLabel += CreateDetectionMessage("detection", "half SR", detecteeActiveStat, detecteeSoldier.AvoidanceActiveStat(detecteeActiveStat), movingSoldierMultipliers[1], movingSoldier.stats.P.Val);
                                detectedRight = true;
                            }
                        }
                        else if (movingSoldier.PhysicalObjectWithinMaxRadius(detecteeSoldier))
                        {
                            addDetection = true;

                            if (detecteeSoldier.AvoidanceActiveStat(detecteeActiveStat) > movingSoldier.DetectionActiveStat(movingSoldierMultipliers[2]))
                            {
                                detectorLabel += CreateDetectionMessage("avoidance", "full SR", detecteeActiveStat, detecteeSoldier.AvoidanceActiveStat(detecteeActiveStat), movingSoldierMultipliers[2], movingSoldier.stats.P.Val);
                                avoidanceRight = true;
                            }
                            else
                            {
                                detectorLabel += CreateDetectionMessage("detection", "full SR", detecteeActiveStat, detecteeSoldier.AvoidanceActiveStat(detecteeActiveStat), movingSoldierMultipliers[2], movingSoldier.stats.P.Val);
                                detectedRight = true;
                            }
                        }
                        else if (movingSoldierOldPosition != Vector3.zero && GlimpseDetectionMovingSeesDetectee(movingSoldier, detecteeSoldier, movingSoldierOldPosition))
                        {
                            addDetection = true;

                            if (detecteeSoldier.AvoidanceActiveStat(detecteeActiveStat) > movingSoldier.DetectionActiveStat(movingSoldierMultipliers[2]))
                            {
                                if (boundCrossTwo != Vector3.zero)
                                    detectorLabel += CreateDetectionMessage("glimpseavoidance", "full SR", detecteeActiveStat, detecteeSoldier.AvoidanceActiveStat(detecteeActiveStat), movingSoldierMultipliers[2], movingSoldier.stats.P.Val, boundCrossOne, boundCrossTwo);
                                else
                                    detectorLabel += CreateDetectionMessage("glimpseavoidance", "full SR", detecteeActiveStat, detecteeSoldier.AvoidanceActiveStat(detecteeActiveStat), movingSoldierMultipliers[2], movingSoldier.stats.P.Val, boundCrossOne);

                                avoidanceRight = true;
                            }
                            else
                            {
                                if (boundCrossTwo != Vector3.zero)
                                    detectorLabel += CreateDetectionMessage("glimpsedetection", "full SR", detecteeActiveStat, detecteeSoldier.AvoidanceActiveStat(detecteeActiveStat), movingSoldierMultipliers[2], movingSoldier.stats.P.Val, boundCrossOne, boundCrossTwo);
                                else
                                    detectorLabel += CreateDetectionMessage("glimpsedetection", "full SR", detecteeActiveStat, detecteeSoldier.AvoidanceActiveStat(detecteeActiveStat), movingSoldierMultipliers[2], movingSoldier.stats.P.Val, boundCrossOne);

                                detectedRight = true;
                            }
                        }
                        else
                        {
                            detectorLabel += "Not detected\n(out of SR)";
                            noDetectRight = true;
                        }
                    }
                    else
                    {
                        detectorLabel += "Not detected\n(blind)";
                        noDetectRight = true;
                    }

                    //check static soldiers seeing moving soldier
                    if (detecteeSoldier.IsAbleToSee())
                    {
                        if (detecteeSoldier.PhysicalObjectWithinMinRadius(movingSoldier))
                        {
                            addDetection = true;

                            if (movingSoldier.AvoidanceActiveStat(movingSoldierActiveStat) > detecteeSoldier.DetectionActiveStat(detecteeMultipliers[0]))
                            {
                                counterLabel += CreateDetectionMessage("avoidance", "3cm", movingSoldierActiveStat, movingSoldier.AvoidanceActiveStat(movingSoldierActiveStat), detecteeMultipliers[0], detecteeSoldier.stats.P.Val);
                                avoidanceLeft = true;
                            }
                            else
                            {
                                if (detecteeSoldier.IsOnOverwatch() && triggersOverwatch)
                                {
                                    if (detecteeSoldier.PhysicalObjectWithinOverwatchCone(movingSoldier))
                                    {
                                        counterLabel += CreateDetectionMessage("overwatch", "3cm", movingSoldierActiveStat, movingSoldier.AvoidanceActiveStat(movingSoldierActiveStat), detecteeMultipliers[0], detecteeSoldier.stats.P.Val);
                                        overwatchLeft = true;
                                    }
                                    else if (GlimpseDetectionDetecteeSeesMovingThroughOverwatchCone(movingSoldier, detecteeSoldier, movingSoldierOldPosition))
                                    {
                                        if (boundCrossTwo != Vector3.zero)
                                            counterLabel += CreateDetectionMessage("overwatch", "full SR", movingSoldierActiveStat, movingSoldier.AvoidanceActiveStat(movingSoldierActiveStat), detecteeMultipliers[2], detecteeSoldier.stats.P.Val, boundCrossOne, boundCrossTwo);
                                        else
                                            counterLabel += CreateDetectionMessage("overwatch", "full SR", movingSoldierActiveStat, movingSoldier.AvoidanceActiveStat(movingSoldierActiveStat), detecteeMultipliers[2], detecteeSoldier.stats.P.Val, boundCrossOne);

                                        overwatchLeft = true;
                                    }
                                    else
                                    {
                                        counterLabel += "Not detected\n(out of overwatch cone)";
                                        noDetectLeft = true;
                                    }
                                }
                                else
                                {
                                    counterLabel += CreateDetectionMessage("detection", "3cm", movingSoldierActiveStat, movingSoldier.AvoidanceActiveStat(movingSoldierActiveStat), detecteeMultipliers[0], detecteeSoldier.stats.P.Val);
                                    detectedLeft = true;
                                }
                            }
                        }
                        else if (detecteeSoldier.PhysicalObjectWithinHalfRadius(movingSoldier))
                        {
                            addDetection = true;

                            if (movingSoldier.AvoidanceActiveStat(movingSoldierActiveStat) > detecteeSoldier.DetectionActiveStat(detecteeMultipliers[1]))
                            {
                                counterLabel += CreateDetectionMessage("avoidance", "half SR", movingSoldierActiveStat, movingSoldier.AvoidanceActiveStat(movingSoldierActiveStat), detecteeMultipliers[1], detecteeSoldier.stats.P.Val);
                                avoidanceLeft = true;
                            }
                            else
                            {
                                if (detecteeSoldier.IsOnOverwatch() && triggersOverwatch)
                                {
                                    if (detecteeSoldier.PhysicalObjectWithinOverwatchCone(movingSoldier))
                                    {
                                        counterLabel += CreateDetectionMessage("overwatch", "half SR", movingSoldierActiveStat, movingSoldier.AvoidanceActiveStat(movingSoldierActiveStat), detecteeMultipliers[1], detecteeSoldier.stats.P.Val);
                                        overwatchLeft = true;
                                    }
                                    else if (GlimpseDetectionDetecteeSeesMovingThroughOverwatchCone(movingSoldier, detecteeSoldier, movingSoldierOldPosition))
                                    {
                                        if (boundCrossTwo != Vector3.zero)
                                            counterLabel += CreateDetectionMessage("overwatch", "full SR", movingSoldierActiveStat, movingSoldier.AvoidanceActiveStat(movingSoldierActiveStat), detecteeMultipliers[2], detecteeSoldier.stats.P.Val, boundCrossOne, boundCrossTwo);
                                        else
                                            counterLabel += CreateDetectionMessage("overwatch", "full SR", movingSoldierActiveStat, movingSoldier.AvoidanceActiveStat(movingSoldierActiveStat), detecteeMultipliers[2], detecteeSoldier.stats.P.Val, boundCrossOne);

                                        overwatchLeft = true;
                                    }
                                    else
                                    {
                                        counterLabel += "Not detected\n(out of overwatch cone)";
                                        noDetectLeft = true;
                                    }
                                }
                                else
                                {
                                    counterLabel += CreateDetectionMessage("detection", "half SR", movingSoldierActiveStat, movingSoldier.AvoidanceActiveStat(movingSoldierActiveStat), detecteeMultipliers[1], detecteeSoldier.stats.P.Val);
                                    detectedLeft = true;
                                }
                            }
                        }
                        else if (detecteeSoldier.PhysicalObjectWithinMaxRadius(movingSoldier))
                        {
                            addDetection = true;

                            if (movingSoldier.AvoidanceActiveStat(movingSoldierActiveStat) > detecteeSoldier.DetectionActiveStat(detecteeMultipliers[2]))
                            {
                                counterLabel += CreateDetectionMessage("avoidance", "full SR", movingSoldierActiveStat, movingSoldier.AvoidanceActiveStat(movingSoldierActiveStat), detecteeMultipliers[2], detecteeSoldier.stats.P.Val);
                                avoidanceLeft = true;
                            }
                            else
                            {
                                if (detecteeSoldier.IsOnOverwatch() && triggersOverwatch)
                                {
                                    if (detecteeSoldier.PhysicalObjectWithinOverwatchCone(movingSoldier))
                                    {
                                        counterLabel += CreateDetectionMessage("overwatch", "full SR", movingSoldierActiveStat, movingSoldier.AvoidanceActiveStat(movingSoldierActiveStat), detecteeMultipliers[2], detecteeSoldier.stats.P.Val);
                                        overwatchLeft = true;
                                    }
                                    else if (GlimpseDetectionDetecteeSeesMovingThroughOverwatchCone(movingSoldier, detecteeSoldier, movingSoldierOldPosition))
                                    {
                                        if (boundCrossTwo != Vector3.zero)
                                            counterLabel += CreateDetectionMessage("overwatch", "full SR", movingSoldierActiveStat, movingSoldier.AvoidanceActiveStat(movingSoldierActiveStat), detecteeMultipliers[2], detecteeSoldier.stats.P.Val, boundCrossOne, boundCrossTwo);
                                        else
                                            counterLabel += CreateDetectionMessage("overwatch", "full SR", movingSoldierActiveStat, movingSoldier.AvoidanceActiveStat(movingSoldierActiveStat), detecteeMultipliers[2], detecteeSoldier.stats.P.Val, boundCrossOne);

                                        overwatchLeft = true;
                                    }
                                    else
                                    {
                                        counterLabel += "Not detected\n(out of overwatch cone)";
                                        noDetectLeft = true;
                                    }
                                }
                                else
                                {
                                    counterLabel += CreateDetectionMessage("detection", "full SR", movingSoldierActiveStat, movingSoldier.AvoidanceActiveStat(movingSoldierActiveStat), detecteeMultipliers[2], detecteeSoldier.stats.P.Val);
                                    detectedLeft = true;
                                }
                            }
                        }
                        else if (movingSoldierOldPosition != Vector3.zero && GlimpseDetectionDetecteeSeesMoving(movingSoldier, detecteeSoldier, movingSoldierOldPosition))
                        {
                            addDetection = true;

                            if (movingSoldier.AvoidanceActiveStat(movingSoldierActiveStat) > detecteeSoldier.DetectionActiveStat(detecteeMultipliers[2]))
                            {
                                if (boundCrossTwo != Vector3.zero)
                                    counterLabel += CreateDetectionMessage("glimpseavoidance", "full SR", movingSoldierActiveStat, movingSoldier.AvoidanceActiveStat(movingSoldierActiveStat), detecteeMultipliers[2], detecteeSoldier.stats.P.Val, boundCrossOne, boundCrossTwo);
                                else
                                    counterLabel += CreateDetectionMessage("glimpseavoidance", "full SR", movingSoldierActiveStat, movingSoldier.AvoidanceActiveStat(movingSoldierActiveStat), detecteeMultipliers[2], detecteeSoldier.stats.P.Val, boundCrossOne);

                                avoidanceLeft = true;
                            }
                            else if (GlimpseDetectionDetecteeSeesMovingThroughOverwatchCone(movingSoldier, detecteeSoldier, movingSoldierOldPosition))
                            {
                                if (boundCrossTwo != Vector3.zero)
                                    counterLabel += CreateDetectionMessage("overwatch", "full SR", movingSoldierActiveStat, movingSoldier.AvoidanceActiveStat(movingSoldierActiveStat), detecteeMultipliers[2], detecteeSoldier.stats.P.Val, boundCrossOne, boundCrossTwo);
                                else
                                    counterLabel += CreateDetectionMessage("overwatch", "full SR", movingSoldierActiveStat, movingSoldier.AvoidanceActiveStat(movingSoldierActiveStat), detecteeMultipliers[2], detecteeSoldier.stats.P.Val, boundCrossOne);
                                    
                                overwatchLeft = true;
                            }
                            else
                            {
                                if (boundCrossTwo != Vector3.zero)
                                    counterLabel += CreateDetectionMessage("glimpsedetection", "full SR", movingSoldierActiveStat, movingSoldier.AvoidanceActiveStat(movingSoldierActiveStat), detecteeMultipliers[2], detecteeSoldier.stats.P.Val, boundCrossOne, boundCrossTwo);
                                else
                                    counterLabel += CreateDetectionMessage("glimpsedetection", "full SR", movingSoldierActiveStat, movingSoldier.AvoidanceActiveStat(movingSoldierActiveStat), detecteeMultipliers[2], detecteeSoldier.stats.P.Val, boundCrossOne);

                                detectedLeft = true;
                            }
                        }
                        else
                        {
                            counterLabel += "Not detected\n(out of SR)";
                            noDetectLeft = true;
                        }
                    }
                    else
                    {
                        counterLabel += "Not detected\n(blind)";
                        noDetectLeft = true;
                    }

                    //determine what kind of arrow to place
                    if (detectedRight)
                    {
                        if (avoidanceLeft)
                            arrowType = "avoidance2WayLeft";
                        else if (detectedLeft)
                            arrowType = "detection2Way";
                        else if (noDetectLeft)
                            arrowType = "detection1WayRight";
                        else if (overwatchLeft)
                            arrowType = "detectionOverwatch2WayLeft";
                    }
                    else if (avoidanceRight)
                    {
                        if (avoidanceLeft)
                            arrowType = "avoidance2Way";
                        else if (detectedLeft)
                            arrowType = "avoidance2WayRight";
                        else if (noDetectLeft)
                            arrowType = "avoidance1WayRight";
                        else if (overwatchLeft)
                            arrowType = "avoidanceOverwatch2WayLeft";
                    }
                    else if (noDetectRight)
                    {
                        if (avoidanceLeft)
                            arrowType = "avoidance1WayLeft";
                        else if (detectedLeft)
                            arrowType = "detection1WayLeft";
                        else if (noDetectLeft)
                            arrowType = "noDetect2Way";
                        else if (overwatchLeft)
                            arrowType = "overwatch1WayLeft";
                    }
                    else if (overwatchRight)
                    {
                        if (avoidanceLeft)
                            arrowType = "avoidanceOverwatch2WayRight";
                        else if (detectedLeft)
                            arrowType = "detectionOverwatch2WayRight";
                        else if (noDetectLeft)
                            arrowType = "overwatch1WayRight";
                    }

                    //suppress detection alerts that evaluate the same as prior state on stat changes which can not affect LOS
                    if (causeOfLosCheck.Equals("statChange"))
                    {
                        if (arrowType == "detection2Way" && movingSoldier.RevealedBySoldiers.Contains(detecteeSoldier.id) && detecteeSoldier.RevealedBySoldiers.Contains(movingSoldier.id))
                        {
                            addDetection = false;
                            //print(arrowType + ": soldiers can already see each other");
                        }
                        else if (arrowType == "detection1WayRight" && detecteeSoldier.RevealedBySoldiers.Contains(movingSoldier.id))
                        {
                            addDetection = false;
                            //print(arrowType + ": onturn can see offturn (1 way)");
                        }
                        else if (arrowType == "detection1WayLeft" && movingSoldier.RevealedBySoldiers.Contains(detecteeSoldier.id))
                        {
                            addDetection = false;
                            //print(arrowType + ": offturn can see onturn (1 way)");
                        }
                    }

                    if (addDetection)
                    {
                        menu.AddDetectionAlert(movingSoldier, detecteeSoldier, detectorLabel, counterLabel, arrowType);
                        showDetectionUI = true;
                    }
                }
                else if (detectee is Claymore detecteeClaymore && !detecteeClaymore.revealed && movingSoldier.IsOppositeTeamAs(detecteeClaymore.placedBy) && movingSoldier.stats.P.Val > detecteeClaymore.ActiveC && movingSoldier.stats.P.Val > detecteeClaymore.ActiveF)
                {
                    if (movingSoldier.PhysicalObjectWithinMaxRadius(detecteeClaymore))
                    {
                        counterLabel = CreateDetectionMessageClaymore(detecteeClaymore.ActiveC, detecteeClaymore.ActiveF, movingSoldier.stats.P.Val);
                        menu.AddDetectionAlert(movingSoldier, detecteeClaymore, "Not detected\n(N/A)", counterLabel, "detection1WayRight");
                        showDetectionUI = true;

                        print($"{movingSoldier.soldierName} detected the claymore at {detecteeClaymore.x},{detecteeClaymore.y},{detecteeClaymore.z}");
                    }
                }
            }

            if (showDetectionUI)
                menu.OpenGMAlertDetectionUI();
            else
                menu.ConfirmDetections(); //required to kill old LOS if soldier moves out of everyone's vis


            //run melee if required (Should wait till detections resolved)
            if (launchMelee != string.Empty)
                StartCoroutine(menu.OpenMeleeUI(launchMelee));
        }
    }

    public string CreateDetectionMessageClaymore(int claymoreC, int claymoreF, int soldierP)
    {
        return $"<color=red>DETECTED</color>\nClaymore C={claymoreC}, F={claymoreF} did not exceed P={soldierP}";
    }
    public string CreateDetectionMessage(string type, string distance, string activeStatName, int activeStatValue, int perceptivenessMultiplier, int soldierPerceptiveness)
    {
        string title, part1, join, part2;

        part1 = $"(within {distance} {activeStatName}={activeStatValue}";
        part2 = $"{perceptivenessMultiplier}P={soldierPerceptiveness * perceptivenessMultiplier})";

        if (type.Contains("avoidance"))
        {
            title = "<color=green>AVOIDANCE</color>\n";
            join = " exceeded ";
        }
        else if (type.Contains("overwatch"))
        {
            title = "<color=yellow>OVERWATCH</color>\n";
            join = " did not exceed ";
        }
        else
        {
            title = "<color=red>DETECTED</color>\n";
            join = " did not exceed ";
        }

        return $"{title}{part1}{join}{part2}";
    }
    public string CreateDetectionMessage(string type, string distance, string activeStatName, int activeStatValue, int perceptivenessMultiplier, int soldierPerceptiveness, Vector3 boundCrossOne)
    {
        string title, part1, join, part2;

        part1 = $"(Until: {boundCrossOne.x}, {boundCrossOne.y}, {boundCrossOne.z})\n(within {distance} {activeStatName}={activeStatValue}";
        part2 = $"{perceptivenessMultiplier}P={soldierPerceptiveness * perceptivenessMultiplier})";
        
        if (type.Contains("avoidance"))
        {
            title = "<color=green>RETREAT AVOIDANCE</color>\n";
            join = " exceeded ";
        }
        else if (type.Contains("overwatch"))
        {
            title = "<color=yellow>RETREAT OVERWATCH</color>\n";
            join = " did not exceed ";
        }
        else
        {
            title = "<color=red>RETREAT DETECTED</color>\n";
            join = " did not exceed ";
        }

        return $"{title}{part1}{join}{part2}";
    }
    public string CreateDetectionMessage(string type, string distance, string activeStatName, int activeStatValue, int perceptivenessMultiplier, int soldierPerceptiveness, Vector3 boundCrossOne, Vector3 boundCrossTwo)
    {
        string title, part1, join, part2;

        part1 = $"(Between: {boundCrossOne.x}, {boundCrossOne.y}, {boundCrossOne.z} and {boundCrossTwo.x}, {boundCrossTwo.y}, {boundCrossTwo.z})\n(within {distance} {activeStatName}={activeStatValue}";
        part2 = $"{perceptivenessMultiplier}P={soldierPerceptiveness * perceptivenessMultiplier})";

        if (type.Contains("avoidance"))
        {
            title = "<color=green>GLIMPSE AVOIDANCE</color>\n";
            join = " exceeded ";
        }
        else if (type.Contains("overwatch"))
        {
            title = "<color=yellow>GLIMPSE OVERWATCH</color>\n";
            join = " did not exceed ";
        }
        else
        {
            title = "<color=red>GLIMPSE DETECTED</color>\n";
            join = " did not exceed ";
        }

        return $"{title}{part1}{join}{part2}";
    }

    public void DetectionAlertAllNonCoroutine()
    {
        StartCoroutine(DetectionAlertAll("losChange", true));
    }
    public IEnumerator DetectionAlertAll(string causeOfLosCheck, bool triggersOverwatch)
    {
        yield return new WaitUntil(() => menu.meleeResolvedFlag == true && menu.inspirerResolvedFlag == true);

        foreach (Soldier s in AllSoldiers())
        {
            if (s.IsOnturnAndAlive())
            {
                //print("Running detection alert for " + s.soldierName);
                StartCoroutine(DetectionAlertSingle(s, causeOfLosCheck, Vector3.zero, string.Empty, triggersOverwatch));
            }
        }
        menu.ConfirmDetections();
    }







    //insert game objects functions
    public void ConfirmInsertGameObjects()
    {
        TMP_Dropdown terminalTypeDropdown = menu.overrideInsertObjectsUI.transform.Find("OptionPanel").Find("TerminalType").Find("TerminalTypeDropdown").GetComponent<TMP_Dropdown>();
        Transform gbItemsPanel = menu.overrideInsertObjectsUI.transform.Find("OptionPanel").Find("AllItemsPanel").Find("Viewport").Find("GoodyBoxItemsContent");
        TMP_InputField claymoreFacingXInput = menu.overrideInsertObjectsUI.transform.Find("OptionPanel").Find("ClaymoreFacing").Find("ClaymoreFacing").Find("XPos").GetComponent<TMP_InputField>();
        TMP_InputField claymoreFacingYInput = menu.overrideInsertObjectsUI.transform.Find("OptionPanel").Find("ClaymoreFacing").Find("ClaymoreFacing").Find("YPos").GetComponent<TMP_InputField>();

        int spawnedObject = menu.overrideInsertObjectsUI.transform.Find("OptionPanel").Find("ObjectType").Find("ObjectTypeDropdown").GetComponent<TMP_Dropdown>().value;
        //check input formatting
        if (spawnedObject != 0 && GetInsertLocation(out Tuple<Vector3, string> spawnLocation))
        {
            if (spawnedObject == 1)
            {
                GoodyBox gb = Instantiate(poiManager.gbPrefab).Init(spawnLocation);
                //fill gb with items
                foreach (Transform child in gbItemsPanel)
                {
                    ItemIconGB itemIcon = child.GetComponent<ItemIconGB>();
                    if (itemIcon != null && itemIcon.pickupNumber > 0)
                        for (int i = 0; i < child.GetComponent<ItemIconGB>().pickupNumber; i++)
                            gb.Inventory.AddItem(itemManager.SpawnItem(child.gameObject.name));
                }
            }
            else if (spawnedObject == 2)
                Instantiate(poiManager.terminalPrefab).Init(spawnLocation, terminalTypeDropdown.options[terminalTypeDropdown.value].text);
            else if (spawnedObject == 3)
                Instantiate(poiManager.barrelPrefab).Init(spawnLocation);
            else if (spawnedObject == 4)
            {
                if (claymoreFacingXInput.textComponent.color == menu.normalTextColour && claymoreFacingYInput.textComponent.color == menu.normalTextColour && int.TryParse(claymoreFacingXInput.text, out int facingXInt) && int.TryParse(claymoreFacingYInput.text, out int facingYInt))
                    Instantiate(poiManager.claymorePrefab).Init(spawnLocation, Tuple.Create(1, 1, facingXInt, facingYInt, "")).PlaceClaymore();
            }

            menu.CloseOverrideInsertObjectsUI();
        }
        else
            print("Invalid Input");
    }
    public bool GetInsertLocation(out Tuple<Vector3, string> insertLocation)
    {
        insertLocation = default;
        if (menu.overrideInsertObjectsUI.transform.Find("OptionPanel").Find("Location").Find("XPos").GetComponent<TMP_InputField>().textComponent.GetComponent<TextMeshProUGUI>().color == menu.normalTextColour &&
            menu.overrideInsertObjectsUI.transform.Find("OptionPanel").Find("Location").Find("YPos").GetComponent<TMP_InputField>().textComponent.GetComponent<TextMeshProUGUI>().color == menu.normalTextColour &&
            menu.overrideInsertObjectsUI.transform.Find("OptionPanel").Find("Location").Find("ZPos").GetComponent<TMP_InputField>().textComponent.GetComponent<TextMeshProUGUI>().color == menu.normalTextColour &&
            menu.overrideInsertObjectsUI.transform.Find("OptionPanel").Find("Terrain").Find("TerrainDropdown").GetComponent<TMP_Dropdown>().value != 0)
        {
            insertLocation = Tuple.Create(new Vector3(int.Parse(menu.overrideInsertObjectsUI.transform.Find("OptionPanel").Find("Location").Find("XPos").GetComponent<TMP_InputField>().text),
                int.Parse(menu.overrideInsertObjectsUI.transform.Find("OptionPanel").Find("Location").Find("YPos").GetComponent<TMP_InputField>().text),
                int.Parse(menu.overrideInsertObjectsUI.transform.Find("OptionPanel").Find("Location").Find("ZPos").GetComponent<TMP_InputField>().text)),
                menu.overrideInsertObjectsUI.transform.Find("OptionPanel").Find("Terrain").Find("TerrainDropdown").GetComponent<TMP_Dropdown>().options[menu.overrideInsertObjectsUI.transform.Find("OptionPanel").Find("Terrain").Find("TerrainDropdown").GetComponent<TMP_Dropdown>().value].text);

            return true;
        }

        return false;
    }
    public void UpdateInsertGameObjectsUI()
    {
        int spawnedObject = menu.overrideInsertObjectsUI.transform.Find("OptionPanel").Find("ObjectType").Find("ObjectTypeDropdown").GetComponent<TMP_Dropdown>().value;
        GameObject terminalType = menu.overrideInsertObjectsUI.transform.Find("OptionPanel").Find("TerminalType").gameObject;
        GameObject allItemsPanel = menu.overrideInsertObjectsUI.transform.Find("OptionPanel").Find("AllItemsPanel").gameObject;
        GameObject claymoreFacing = menu.overrideInsertObjectsUI.transform.Find("OptionPanel").Find("ClaymoreFacing").gameObject;
        terminalType.SetActive(false);
        allItemsPanel.SetActive(false);
        claymoreFacing.SetActive(false);

        if (spawnedObject == 1)
        {
            allItemsPanel.SetActive(true);
            activeItemPanel = allItemsPanel.transform;
        }
        else if (spawnedObject == 2)
        {
            terminalType.SetActive(true);
        }
        else if (spawnedObject == 4)
        {
            claymoreFacing.SetActive(true);
        }
    }





    public void LoadData(GameData data)
    {
        currentRound = data.currentRound;
        currentTeam = data.currentTeam;

        battlefield.transform.position = data.mapPosition;
        battlefield.transform.localScale = data.mapDimensions;

        bottomPlane.transform.position = data.bottomPlanePosition;
        bottomPlane.transform.localScale = data.bottomPlaneDimensions;

        outlineArea.transform.position = data.outlineAreaPosition;
        outlineArea.transform.localScale = data.outlineAreaDimensions;

        cam.transform.position = data.camPosition;
        cam.orthographicSize = data.camOrthoSize;
        sun.transform.position = data.sunPosition;
        maxRounds = data.maxRounds;
        maxTurnTime = data.maxTurnTime;
        maxTeams = data.maxTeams;
        maxX = data.maxX;
        maxY = data.maxY;
        maxZ = data.maxZ;
    }
    public void SaveData(ref GameData data)
    {
        data.currentRound = currentRound;
        data.currentTeam = currentTeam;

        data.mapPosition = battlefield.transform.position;
        data.mapDimensions = battlefield.transform.localScale;
        data.camPosition = cam.transform.position;
        data.camOrthoSize = cam.orthographicSize;
        data.sunPosition = sun.transform.position;
        data.maxRounds = maxRounds;
        data.maxTurnTime = maxTurnTime;
        data.maxTeams = maxTeams;
        data.maxX = maxX;
        data.maxY = maxY;
        data.maxZ = maxZ;
    }
}
