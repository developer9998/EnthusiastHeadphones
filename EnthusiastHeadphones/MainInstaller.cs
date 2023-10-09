using EnthusiastHeadphones.Behaviors;
using GorillaLocomotion;
using UnityEngine;
using Zenject;

namespace EnthusiastHeadphones
{
    public class MainInstaller : Installer
    {
        public GameObject Player => Object.FindObjectOfType<Player>().gameObject;

        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<Main>().FromNewComponentOn(Player).AsSingle();
            Container.Bind<AssetLoader>().AsSingle();
        }
    }
}
