using UnityEngine;

namespace Tetrage.Services{
    public class DisableOnAwake : MonoBehaviour{

        [SerializeField] private bool _disableOnAwake = true;

        private void Awake(){

            if(_disableOnAwake){
                this.gameObject.SetActive(false);
            }

        }
    }
}