using Unity.Netcode;

/*
    This component should be attached to objects that require health, as well as a health component. It can also be derived from 
    such as PlayerStats and EnemyStats for objects that require more than health
*/
public class NetworkStats: NetworkBehaviour
{    
    public NetworkStat HealthStat;

}
