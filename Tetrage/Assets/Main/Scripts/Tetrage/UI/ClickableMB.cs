using UnityEngine;
using UnityEngine.EventSystems;
using System;
using R3;
using System.Collections.Generic;

namespace Tetrage.UI
{
    public class ClickableMB : MonoBehaviour, IPointerClickHandler
    {
        public Observable<Unit> Clicked => _clicked;
        private readonly Subject<Unit> _clicked = new();

        public void OnPointerClick(PointerEventData eventData){
            Debug.Log($"ClickableMB: OnPointerClick {eventData.pointerId}");
            _clicked.OnNext(Unit.Default);
        }

        private void OnDestroy()
        {
            Dispose();
        }
        public void Dispose()
        {
            _clicked.OnCompleted();
        }
    }
}

