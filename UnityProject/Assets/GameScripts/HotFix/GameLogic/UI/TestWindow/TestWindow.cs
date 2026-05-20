using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
	[Window(UILayer.UI, location : "TestWindow")]
	public partial class TestWindow
	{
		private const string PlayerTypeName = "Player";
		private const string ReusableDataMemberName = "ReusableData";

		private MonoBehaviour _playerBehaviour;
		private object _playerReusableData;
		private bool _cursorShown;
		private bool _loggedMissingPlayer;

		protected override void OnCreate()
		{
			m_sliderTime.minValue = 0f;
			m_sliderTime.maxValue = Mathf.Max(1f, m_sliderTime.maxValue);
			m_sliderTime.wholeNumbers = false;
			m_tmpTip.text = "Alt呼出鼠标，Tab自动锁敌";

			ApplyCursorState(false);
			RefreshTimeScale(1f);
			RefreshPlayerInfo();
		}

		protected override void OnRefresh()
		{
			m_sliderTime.SetValueWithoutNotify(1f);
			RefreshTimeScale(1f);
			RefreshPlayerInfo();
		}

		protected override void OnUpdate()
		{
			UpdateCursorState();
			RefreshPlayerInfo();
		}

		protected override void OnDestroy()
		{
			ApplyCursorState(false);
		}

		#region 事件

		private partial void OnSliderTimeChange(float value)
		{
			RefreshTimeScale(value);
		}

		#endregion

		private void RefreshTimeScale(float value)
		{
			Time.timeScale = value;
			m_tmpTimeTip.text = $"当前TimeScale:{value:F2}";
		}

		private void UpdateCursorState()
		{
			bool shouldShow = false;
			var keyboard = Keyboard.current;
			if (keyboard != null)
			{
				shouldShow = keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed;
			}

			if (shouldShow != _cursorShown)
			{
				ApplyCursorState(shouldShow);
			}
		}

		private void ApplyCursorState(bool show)
		{
			_cursorShown = show;
			Cursor.visible = show;
			Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
		}

		private void RefreshPlayerInfo()
		{
			if (!TryResolvePlayerReusableData())
			{
				m_tmpAngleTip.text = "目标角度：0°";
				m_tmpStateTip.text = "当前状态：";
				m_tmpSpeedTip.text = "当前速度：";
				m_tmpLockTip.text = "当前锁敌对象：无";
				return;
			}

			float targetAngle = ReadBindableValue(_playerReusableData, "targetAngle", 0f);
			string currentState = ReadBindableValue(_playerReusableData, "currentState", string.Empty);
			Transform lockTarget = ReadBindableValue<Transform>(_playerReusableData, "lockTarget", null);
			float currentSpeed = ReadMemberValue(ReadMemberValue<object>(_playerReusableData, "speedValueParameter", null), "CurrentValue", 0f);

			m_tmpAngleTip.text = $"目标角度：{targetAngle:F2}°";
			m_tmpStateTip.text = $"当前状态：{currentState}";
			m_tmpSpeedTip.text = $"当前速度：{currentSpeed:F2}";
			m_tmpLockTip.text = $"当前锁敌对象：{(lockTarget == null ? "无" : lockTarget.name)}";
		}

		private bool TryResolvePlayerReusableData()
		{
			if (_playerReusableData != null)
			{
				return true;
			}

			if (_playerBehaviour == null)
			{
				var behaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);
				for (int i = 0; i < behaviours.Length; i++)
				{
					var behaviour = behaviours[i];
					if (behaviour == null || behaviour.GetType().Name != PlayerTypeName)
					{
						continue;
					}

					_playerBehaviour = behaviour;
					break;
				}
			}

			if (_playerBehaviour == null)
			{
				if (!_loggedMissingPlayer)
				{
					_loggedMissingPlayer = true;
					Log.Warning("TestWindow 未找到 Player，窗口将先显示默认调试信息。");
				}

				return false;
			}

			_playerReusableData = ReadMemberValue<object>(_playerBehaviour, ReusableDataMemberName, null);
			return _playerReusableData != null;
		}

		private static T ReadBindableValue<T>(object target, string memberName, T fallback)
		{
			object bindable = ReadMemberValue<object>(target, memberName, null);
			if (bindable == null)
			{
				return fallback;
			}

			return ReadMemberValue(bindable, "Value", fallback);
		}

		private static T ReadMemberValue<T>(object target, string memberName, T fallback)
		{
			if (target == null)
			{
				return fallback;
			}

			var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			var type = target.GetType();
			var property = type.GetProperty(memberName, flags);
			if (property != null)
			{
				object value = property.GetValue(target);
				if (value is T typedValue)
				{
					return typedValue;
				}

				if (value != null)
				{
					try
					{
						return (T)Convert.ChangeType(value, typeof(T));
					}
					catch
					{
						return fallback;
					}
				}

				return fallback;
			}

			var field = type.GetField(memberName, flags);
			if (field != null)
			{
				object value = field.GetValue(target);
				if (value is T typedValue)
				{
					return typedValue;
				}

				if (value != null)
				{
					try
					{
						return (T)Convert.ChangeType(value, typeof(T));
					}
					catch
					{
						return fallback;
					}
				}
			}

			return fallback;
		}
	}
}
