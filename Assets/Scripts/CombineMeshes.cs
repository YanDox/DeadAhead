using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombineMeshes : MonoBehaviour
{

	[Tooltip("Если true, объекты после объединения будут уничтожены (удалены из сцены).")]
	public bool destroyCombinedObjects = true;

	[Tooltip("Если true, создаст новый родительский объект для объединённого меша.")]
	public bool createNewParent = true;

	void Start()
	{
		CombineMeshe();
	}
	public void CombineMeshe()
	{
		// Получаем все MeshFilter в дочерних объектах (включая неактивные)
		MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);
		if (meshFilters.Length == 0)
		{
			Debug.LogWarning("Нет MeshFilter для объединения!");
			return;
		}

		// Создаём массив для CombineInstance
		CombineInstance[] combine = new CombineInstance[meshFilters.Length];

		// Материал первого объекта (предполагается, что все материалы одинаковые)
		Material sharedMaterial = null;

		for (int i = 0; i < meshFilters.Length; i++)
		{
			combine[i].mesh = meshFilters[i].sharedMesh;
			combine[i].transform = meshFilters[i].transform.localToWorldMatrix;

			// Получаем материал из первого MeshRenderer
			if (i == 0)
			{
				MeshRenderer meshRenderer = meshFilters[i].GetComponent<MeshRenderer>();
				if (meshRenderer != null)
				{
					sharedMaterial = meshRenderer.sharedMaterial;
				}
			}

			// Деактивируем или уничтожаем исходные объекты
			if (destroyCombinedObjects)
			{
				Destroy(meshFilters[i].gameObject);
			}
			else
			{
				meshFilters[i].gameObject.SetActive(false);
			}
		}

		// Создаём новый объект для объединённого меша (если нужно)
		GameObject combinedMeshGameObject = createNewParent ? new GameObject("CombinedMesh") : gameObject;

		// Добавляем компоненты MeshFilter и MeshRenderer
		MeshFilter combinedMeshFilter = combinedMeshGameObject.AddComponent<MeshFilter>();
		MeshRenderer combinedMeshRenderer = combinedMeshGameObject.AddComponent<MeshRenderer>();

		// Настраиваем объединённый меш
		combinedMeshFilter.mesh = new Mesh();
		combinedMeshFilter.mesh.CombineMeshes(combine);

		// Присваиваем материал (если он был найден)
		if (sharedMaterial != null)
		{
			combinedMeshRenderer.sharedMaterial = sharedMaterial;
		}
		else
		{
			Debug.LogWarning("Материал не найден! Объединённый меш будет без материала.");
		}

		// Оптимизация: сжимаем меш и пересчитываем нормали
		combinedMeshFilter.mesh.Optimize();
		combinedMeshFilter.mesh.RecalculateNormals();

		// Если создан новый родительский объект, делаем его дочерним
		if (createNewParent && combinedMeshGameObject != gameObject)
		{
			combinedMeshGameObject.transform.SetParent(transform);
		}
	}
}
