using System;
using UnityEngine;
public class Stat{
    public event Action<float> OnValueChanged; 
    public event EventHandler OnEqualsZero;
    private float value;
    private float maxValue;

    public Stat(float maxValue){
        value = maxValue;
        this.maxValue = maxValue;
    }
    public float GetValue(){
        Debug.Log("Get Value " + value);
        return value;
    }

    public float GetPercentage(){
        return value/maxValue;
    }
    
    public void Increase(float increaseAmount){
        value += increaseAmount;
        if(value >= maxValue){value = maxValue;}
        OnValueChanged?.Invoke(value);
    }

    public void Decrease(float decreaseAmount){
        value -= decreaseAmount;
        if(value <= 0){
            value = 0;
            OnEqualsZero?.Invoke(this, EventArgs.Empty);
        }
        OnValueChanged?.Invoke(value);
    }

}