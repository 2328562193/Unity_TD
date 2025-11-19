public class Char : MonoBehaviour {

    private List<ForceMove> forceMoves = new List<ForceMove>();
    private List<int> toRemoveForceMoves = new List<int>();

    public void Move(float timePassed){
        Vector2 displacement = Vector2.zero;
        displacement += DoForceMove(timePassed);
        transform.Translate(displacement);
    }

    public Vector2 DoForceMove(float timePassed){
        toRemoveForceMoves.Clear();
        Vector2 forceDisplacement = Vector2.zero;
        int length = forceMoves.Count;
        for(int i = 0; i < length; i++){
            ForceMove forceMove = forceMoves[i];
            forceDisplacement += forceMove.GetDisplacement(timePassed);
            if (forceMove.IsDone()) toRemoveForceMoves.Add(i);
        }
        int toRemoveLength = toRemoveForceMoves.Count;
        for(int i = toRemoveLength - 1; i >= 0; i--){
            forceMoves.RemoveAt(toRemoveForceMoves[i]);
        }
        return forceDisplacement;
    }

    public void AddForceMove(ForceMove move){
        this.forceMoves.Add(move);
    }

    private ChaProperty baseProp = new ChaProperty(100, 100, 0, 20, 100);
    private ChaProperty[] buffProp = new ChaProperty[2]{ChaProperty.zero, ChaProperty.zero};
    private ChaProperty _prop = ChaProperty.zero;

    private void AttrRecheck(){
        for(int i = 0; i < buffProp.Length; i++) buffProp[i] = ChaProperty.zero;
        for(int i = 0; i < this.buff.Count; i++){
            for(int j = 0; j < Mathf.Min(buffProp.Length, buff[i].model.propMod.Length); j++){
                buffProp[j] += buff[i].model.propMod[j] * buff[i].stack;
            }
        }
        this._prop = baseProp + buffProp[0] + (baseProp * buffProp[1]);
    }
}