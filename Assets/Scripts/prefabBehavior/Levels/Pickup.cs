using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pickup : MonoBehaviour
{
    public WeaponHandler WH;
    public WEAPONS pickupType;

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            switch (pickupType)
            {                
                case WEAPONS.RAILGUN:
                    if (!WH.weaponInventory.Contains("Railgun"))
                    {
                        WH.weaponInventory.Add("Railgun");
                        WH.SetRailgun();
                    }
                    break;
                case WEAPONS.SWORD:
                    if (!WH.weaponInventory.Contains("Sword"))
                    {
                        WH.weaponInventory.Add("Sword");
                        WH.SetSword();
                    }
                    break;
                case WEAPONS.SCATTERGUN:
                    if (!WH.weaponInventory.Contains("Scattergun"))
                    {
                        WH.weaponInventory.Add("Scattergun");
                        WH.SetScattergun();
                    }
                    break;
            }
        }
    }

}
