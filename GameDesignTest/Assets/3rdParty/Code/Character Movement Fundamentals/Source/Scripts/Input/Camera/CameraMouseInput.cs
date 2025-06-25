using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CMF
{
    //This camera input class is an example of how to get input from a connected mouse using Unity's default input system;
    //It also includes an optional mouse sensitivity setting;
    public class CameraMouseInput : CameraInput
    {
        [SerializeField] private PlayerInputManager _inputManager;
        //Invert input options;
		public bool invertHorizontalInput = false;
		public bool invertVerticalInput = false;

        Vector2 aimInput;

        private void Awake()
        {
            _inputManager.OnInputReceived += OnInputReceived;
        }

        private void OnInputReceived(ButtonType button, InputType inputType, Vector2? inputAxis)
        {
            switch (button)
            {
                case ButtonType.RStick:
                    {
                        if (inputType == InputType.Hold)
                            aimInput = (Vector2)inputAxis;
                        else if (inputType == InputType.Release)
                            aimInput = Vector3.zero;
                    }
                    break;
            }
        }

        public override float GetHorizontalCameraInput()
        {
            //Get raw mouse input;
            float _input = aimInput.x;
            
            //Since raw mouse input is already time-based, we need to correct for this before passing the input to the camera controller;
            if(Time.timeScale > 0f && Time.deltaTime > 0f)
            {
                _input /= Time.deltaTime;
                _input *= Time.timeScale;
            }
            else
                _input = 0f;

            //Invert input;
            if(invertHorizontalInput)
                _input *= -1f;

            return _input;
        }

        public override float GetVerticalCameraInput()
        {
           //Get raw mouse input;
            float _input = -aimInput.y;
            
            //Since raw mouse input is already time-based, we need to correct for this before passing the input to the camera controller;
            if(Time.timeScale > 0f && Time.deltaTime > 0f)
            {
                _input /= Time.deltaTime;
                _input *= Time.timeScale;
            }
            else
                _input = 0f;

            //Invert input;
            if(invertVerticalInput)
                _input *= -1f;

            return _input;
        }
    }
}
