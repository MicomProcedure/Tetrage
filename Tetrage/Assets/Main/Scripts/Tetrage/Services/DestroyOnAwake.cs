using UnityEngine;

namespace Tetrage.Services{
    public class DestroyOnAwake : MonoBehaviour{

        [SerializeField] private bool _destroyOnAwake = true;

        private void Awake(){

            if(_destroyOnAwake){
                Destroy(this.gameObject);
            }

        }
    }
}