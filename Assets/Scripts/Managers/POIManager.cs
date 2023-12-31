using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class POIManager : MonoBehaviour, IDataPersistence
{
    public List<string> allPOIIds = new();
    public List<POI> allPOIs = new();
    public POI poiPrefab;
    public GoodyBox gbPrefab;
    public Terminal terminalPrefab;
    public ExplosiveBarrel barrelPrefab;
    public Claymore claymorePrefab;
    public SmokeCloud smokeCloudPrefab;
    public TabunCloud tabunCloudPrefab;

    public void LoadData(GameData data)
    {
        //destroy existing POIs ready to regenerate them
        IEnumerable<POI> allPOIsInst = FindObjectsOfType<POI>();
        foreach (POI poi in allPOIsInst)
            Destroy(poi.gameObject);

        allPOIIds = data.allPOIIds;
        foreach (string id in allPOIIds)
        {
            POI newPOI = Instantiate(poiPrefab);
            newPOI.id = id;
            newPOI.LoadData(data);
            //print($"{newPOI.id}: {newPOI.poiType}: {newPOI.GetType()}");
            if (newPOI.poiType == "terminal")
            {
                Destroy(newPOI.gameObject);
                newPOI = Instantiate(terminalPrefab);
                
            }
            else if (newPOI.poiType == "gb")
            {
                Destroy(newPOI.gameObject);
                newPOI = Instantiate(gbPrefab);
            }
            else if (newPOI.poiType == "barrel")
            {
                Destroy(newPOI.gameObject);
                newPOI = Instantiate(barrelPrefab);
            }
            else if (newPOI.poiType == "claymore")
            {
                Destroy(newPOI.gameObject);
                newPOI = Instantiate(claymorePrefab);
            }
            else if (newPOI.poiType == "smoke")
            {
                Destroy(newPOI.gameObject);
                newPOI = Instantiate(smokeCloudPrefab);
            }
            else if (newPOI.poiType == "tabun")
            {
                Destroy(newPOI.gameObject);
                newPOI = Instantiate(tabunCloudPrefab);
            }
            //print($"{newPOI.GetType()}");
            newPOI.id = id;
            newPOI.LoadData(data);
        }
    }

    public void SaveData(ref GameData data)
    {
        allPOIIds.Clear();
        data.allPOIIds.Clear();

        IEnumerable<POI> allPOIs = FindObjectsOfType<POI>();
        foreach (POI poi in allPOIs)
            if (!allPOIIds.Contains(poi.id))
                allPOIIds.Add(poi.id);

        data.allPOIIds = allPOIIds;
    }
    public void DestroyPOI(POI poi)
    {
        Destroy(poi.gameObject);
        RefreshPOIList();
    }
    public void RefreshPOIList()
    {
        allPOIs = FindObjectsOfType<POI>().ToList();
    }
    public POI FindPOIById(string id)
    {
        foreach (POI poi in FindObjectsOfType<POI>())
        {
            if (poi.id == id)
                return poi;
        }
        return null;
    }
}
