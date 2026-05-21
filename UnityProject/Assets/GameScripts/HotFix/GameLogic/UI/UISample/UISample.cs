using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// Enviro 系统 UI 窗口 - 显示时间、天气、环境等信息
    /// </summary>
    [Window(UILayer.UI, location: "UISample")]
    public partial class UISample : UIWindow
    {
        private int _refreshTimerId;
        private int _currentQualityIndex;
        private int _currentWeatherIndex;

        #region 生命周期

        protected override void OnCreate()
        {
            SubscribeEnviroEvents();
            SetupSlider();
            StartRefreshTimer();
            RefreshAllInfo();
        }

        protected override void OnRefresh()
        {
            RefreshAllInfo();
        }

        protected override void OnDestroy()
        {
            CleanupTimer();
            UnsubscribeEnviroEvents();
        }

        #endregion

        #region 事件处理

        protected override void RegisterEvent()
        {
            m_sliderHour_.onValueChanged.AddListener(OnHourSliderChanged);
            m_btnQualityNext_.onClick.AddListener(OnQualityNextClicked);
            m_btnWeatherNext_.onClick.AddListener(OnWeatherNextClicked);
            m_toggleSimulation_.onValueChanged.AddListener(OnSimulationToggled);
        }

        #endregion

        #region Enviro 事件订阅

        private void SubscribeEnviroEvents()
        {
            if (Enviro.EnviroManager.instance == null) return;

            Enviro.EnviroManager.instance.OnHourPassed += OnHourPassed;
            Enviro.EnviroManager.instance.OnWeatherChanged += OnWeatherChanged;
            Enviro.EnviroManager.instance.OnSeasonChanged += OnSeasonChanged;
        }

        private void UnsubscribeEnviroEvents()
        {
            if (Enviro.EnviroManager.instance == null) return;

            Enviro.EnviroManager.instance.OnHourPassed -= OnHourPassed;
            Enviro.EnviroManager.instance.OnWeatherChanged -= OnWeatherChanged;
            Enviro.EnviroManager.instance.OnSeasonChanged -= OnSeasonChanged;
        }

        private void OnHourPassed() => RefreshTimeInfo();
        private void OnWeatherChanged(Enviro.EnviroWeatherType type) => RefreshWeatherInfo();
        private void OnSeasonChanged(Enviro.EnviroEnvironment.Seasons season) => RefreshSeasonInfo();

        #endregion

        #region 定时器

        private void StartRefreshTimer()
        {
            _refreshTimerId = GameModule.Timer.AddTimer(OnRefreshTimer, 2f, isLoop: true);
        }

        private void CleanupTimer()
        {
            if (_refreshTimerId > 0)
            {
                GameModule.Timer.RemoveTimer(_refreshTimerId);
                _refreshTimerId = 0;
            }
        }

        private void OnRefreshTimer(object[] args) => RefreshEnvironmentInfo();

        #endregion

        #region 数据刷新

        private void RefreshAllInfo()
        {
            RefreshTimeInfo();
            RefreshWeatherInfo();
            RefreshEnvironmentInfo();
            RefreshQualityInfo();
        }

        private void RefreshTimeInfo()
        {
            var time = Enviro.EnviroManager.instance?.Time;
            if (time == null) return;

            m_tmpHour_.text = time.GetTimeStringWithSeconds();
            m_tmpDate_.text = $"{time.days:00}/{time.months:00}/{time.years:0000}";
        }

        private void RefreshWeatherInfo()
        {
            var weather = Enviro.EnviroManager.instance?.Weather;
            if (weather?.targetWeatherType == null) return;

            m_tmpWeather_.text = $"Current Weather: {weather.targetWeatherType.name}";
        }

        private void RefreshEnvironmentInfo()
        {
            var env = Enviro.EnviroManager.instance?.Environment;
            if (env == null) return;

            m_tmpTemperature_.text = $"Temperature: {env.Settings.temperature:0.0} °C";
            m_tmpWetness_.text = $"Wetness: {env.Settings.wetness:0.00}";
            m_tmpSnow_.text = $"Snow: {env.Settings.snow:0.00}";

            RefreshSeasonInfo();
        }

        private void RefreshSeasonInfo()
        {
            var env = Enviro.EnviroManager.instance?.Environment;
            if (env == null) return;

            m_tmpSeason_.text = $"Current Season: {GetSeasonName(env.Settings.season)}";
        }

        private void RefreshQualityInfo()
        {
            var quality = Enviro.EnviroManager.instance?.Quality;
            if (quality?.Settings?.defaultQuality == null) return;

            m_tmpQuality_.text = $"Current Quality: {quality.Settings.defaultQuality.name}";
        }

        private static string GetSeasonName(Enviro.EnviroEnvironment.Seasons season) => season switch
        {
            Enviro.EnviroEnvironment.Seasons.Spring => "Spring",
            Enviro.EnviroEnvironment.Seasons.Summer => "Summer",
            Enviro.EnviroEnvironment.Seasons.Autumn => "Autumn",
            Enviro.EnviroEnvironment.Seasons.Winter => "Winter",
            _ => "Unknown"
        };

        #endregion

        #region Slider 回调

        private partial void OnHourSliderChanged(float value)
        {
            var time = Enviro.EnviroManager.instance?.Time;
            if (time == null) return;

            time.SetTimeOfDay(value * 24f);
        }

        #endregion

        #region 按钮回调

        private partial void OnQualityNextClicked()
        {
            var quality = Enviro.EnviroManager.instance?.Quality;
            if (quality?.Settings == null) return;

            var qualities = quality.Settings.Qualities;
            if (qualities == null || qualities.Count == 0) return;

            _currentQualityIndex = (_currentQualityIndex + 1) % qualities.Count;
            quality.Settings.defaultQuality = qualities[_currentQualityIndex];

            RefreshQualityInfo();
        }

        private partial void OnWeatherNextClicked()
        {
            var weather = Enviro.EnviroManager.instance?.Weather;
            if (weather?.Settings == null) return;

            var weatherTypes = weather.Settings.weatherTypes;
            if (weatherTypes == null || weatherTypes.Count == 0) return;

            _currentWeatherIndex = (_currentWeatherIndex + 1) % weatherTypes.Count;
            weather.ChangeWeather(weatherTypes[_currentWeatherIndex]);

            RefreshWeatherInfo();
        }

        private partial void OnSimulationToggled(bool isOn)
        {
            var time = Enviro.EnviroManager.instance?.Time;
            if (time?.Settings == null) return;

            time.Settings.simulate = isOn;
        }

        #endregion

        #region 初始化

        private void SetupSlider()
        {
            m_sliderHour_.minValue = 0f;
            m_sliderHour_.maxValue = 1f;
            m_sliderHour_.wholeNumbers = false;
        }

        #endregion
    }
}
