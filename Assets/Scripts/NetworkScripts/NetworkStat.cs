using System;
using Unity.Netcode;

public class NetworkStat{
    public NetworkVariable<float> MaxStat;
    public NetworkVariable<float> Stat;

    public event EventHandler OnEqualsZero;

    // Constructor takes pre-initialized NetworkVariables
    public NetworkStat(NetworkVariable<float> stat, NetworkVariable<float> maxStat){
        Stat = stat;
        MaxStat = maxStat;
    }

    public void Increase(float amount){
        if (Stat.Value == MaxStat.Value) {return;}
        
        float newVal = Stat.Value + amount;
        if (newVal > MaxStat.Value) {newVal = MaxStat.Value;}
        Stat.Value = newVal;
    }

    public void Decrease(float amount){
        if (Stat.Value == 0) {return;}
        
        float newVal = Stat.Value - amount;
        if (newVal < 0){
            newVal = 0;
            OnEqualsZero?.Invoke(this, EventArgs.Empty);
        }
        Stat.Value = newVal;
    }
}