using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HashValues
{
    private Dictionary<float, int> mapPlayer1 = new Dictionary<float, int>();
    private Dictionary<float, int> mapPlayer2 = new Dictionary<float, int>();

    private Dictionary<float, int> helpCutPlayer1 = new Dictionary<float, int>{
            { 2f, 47 },
            { 4f, 68 },
            { 6f, 81 },
            { 8f, 26 },
            { 10f, 16 }
        };
    private Dictionary<float, int> helpCutPlayer2 = new Dictionary<float, int>{
            { 2f, 44 },
            { 4f, 92 },
            { 6f, 60 },
            { 8f, 32 },
            { 10f, 10 }
        };

    public Dictionary<float, int> CountValues(Dictionary<int, GameObject> squares, int playerNum)
    {
        Dictionary<float, int> resultMap = new Dictionary<float, int>();

        foreach (var it in squares)
        {
            PointOfState p = it.Value.GetComponent<PointOfState>();
            float value = p.getMyVal(playerNum);


            if (resultMap.ContainsKey(value))
            {
                resultMap[value]++;
            }
            else
            {
                resultMap[value] = 1;
            }
        }

        return resultMap;
    }

    public void buildHelp(Dictionary<int, GameObject> squares)
    {
        this.mapPlayer1 = CountValues(squares, 1);
        this.mapPlayer2 = CountValues(squares, 2);
        this.helpCutPlayer1 = buildHelpCutPlayer(this.mapPlayer1);
        this.helpCutPlayer2 = buildHelpCutPlayer(this.mapPlayer2);
    }


    // need key 2 to be even!
    // do a quick help but not true all the time....
    // need key that is even when its divide 2 (6/2=3 is not good for example)
    // return 
    private Dictionary<float, int> buildHelpCutPlayer(Dictionary<float, int> mapPlayer)
    {
        Dictionary<float, int> helpCutPlayer = new Dictionary<float, int>();
        int mod = 0, value=0;
        float key;
        foreach (var it in mapPlayer)
        {
            key= it.Key;
            value= it.Value;
            mod = value % 2;
            // Add the value/2 of the key ({2, 10} -> 5)
            if (helpCutPlayer.ContainsKey(key))
            {
                helpCutPlayer[key]+= value / 2;
            }
            else
            {
                helpCutPlayer[key] = value / 2;
            }

            // Check if mod the add lower number ({4, 13} -> 6 => key[2]++)
            if (mod == 1)
            {
                mod = 0;
                int div = (int)(key / 2);
                if (div % 2 == 0)
                {
                    if (helpCutPlayer.ContainsKey(div))
                    {
                        helpCutPlayer[div] ++;
                    }
                    else
                    {
                        helpCutPlayer[div] = 1;
                    }
                }
            }
        }
        return helpCutPlayer;

    }


    public Dictionary<float, int> getPlayerHelp( int playerNum)
    {
        return playerNum == 1 ? this.helpCutPlayer1 : helpCutPlayer2;
    }


    public void printHelp(int playerNum)
    {
        
        Debug.Log("Player "+ playerNum);
        if (playerNum == 1)
        {
            printPlayer(mapPlayer1, helpCutPlayer1);
        }
        else {
            printPlayer(mapPlayer2, helpCutPlayer2);
        }

    }
    private void printPlayer(Dictionary<float, int> map, Dictionary<float, int> helpCut)
    {
        float sumsum = 0;
        foreach (var it in map)
        {
            Debug.Log(it);
            sumsum += it.Key * it.Value;

        }
        Debug.Log("sum: " + sumsum);
        sumsum = 0;

        foreach (var it in helpCut)
        {
            Debug.Log(it);
            sumsum += it.Key * it.Value;

        }
        Debug.Log("divide: " + sumsum);
    }
}
