using System.Collections.Generic;

public class ComboSystem
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public enum AttackPressType{
        Quick,
        Hold
    }

    [System.Serializable]
    public struct ComboStep{
        public AttackSO attack;
        public AttackPressType userPressType;
        public GameInput.AttackInput userInput;
    }

    [System.Serializable]
    public class Combo{
        public List<ComboStep> comboSteps = new List<ComboStep>();
        public int currIndex {get; private set;}
        public ComboStep currComboStep {get; private set;}
        public void Initialize(){
            if (comboSteps.Count > 0){
                currIndex = 0;
                currComboStep = comboSteps[currIndex];
            }
        }

        public void UpdateComboStep(){
            currIndex = (currIndex + 1) % comboSteps.Count;
            currComboStep = comboSteps[currIndex];
        }
        public void ResetComboStep(){
            currIndex = 0;
            currComboStep = comboSteps[currIndex];
        }
    }
}