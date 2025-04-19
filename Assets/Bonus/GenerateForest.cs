using System.Collections.Generic;
using UnityEngine;

public class GenerateForest : MonoBehaviour
{
    [Tooltip("Rende visibile i log di debug")]
    public bool debug = true;

    public Material[] materials = new Material[5];
    public GameObject[] albero = new GameObject[4];
    [Range(5, 20)]
    public int numeroAlberi = 10;
    [Tooltip("Raggio della foresta da generare, cambiare con cautela")]
    public float raggioForesta = 5f;
    [Tooltip("Il nome della chioma dell'albero deve essere lo stesso per ogni GameObject inserito nell'array albero")]
    public string nomeChioma = "Chioma";
    List<GameObject> alberiForesta = new List<GameObject>();
    List<Vector3> posizioniAlberi = new List<Vector3>();

    //Potevo usare gli eventi ... ma per provarlo ho preferito fare così
    [Tooltip("Impostare VERO per ricalcolare lo script")]
    public bool rigera = false;

    private void Forest()
    {
        GameObject nuovoAlbero;
        MeshRenderer chiomaMR;
        
        int randomTipoAlbero = 0;
        int randomMaterial = 0;

        //Resetto le liste
        alberiForesta.Clear();
        posizioniAlberi.Clear();

        //Genero la posizione degli alberi
        PosizioniValideAlberi();

        //Genero la foresta
        for (int i = 0; i < numeroAlberi; i++)
        {
            //Scelgo una variante dell'albero e lo istanzio
            randomTipoAlbero = Random.Range(0, albero.Length);
            nuovoAlbero = Instantiate(albero[randomTipoAlbero], posizioniAlberi[i], Quaternion.identity);
            alberiForesta.Add(nuovoAlbero);

            //Assegno un nuovo material alla chioma dell'albero appena istanziato
            chiomaMR = nuovoAlbero.transform.Find(nomeChioma).GetComponent<MeshRenderer>();
            randomMaterial = Random.Range(0, materials.Length);
            chiomaMR.material = materials[randomMaterial];
        }
    }

    //Genero una posizione random per un albero
    private Vector3 PosizioneAlbero()
    {
        Vector2 posizioneRandom = Vector2.zero;
        float moltiplicatore = 0f;

        posizioneRandom = Random.insideUnitSphere;
        moltiplicatore = Random.Range(0f, raggioForesta);
        posizioneRandom *= moltiplicatore;
        return new Vector3(posizioneRandom.x, 0f, posizioneRandom.y);
    }

    //Genero tutte le posizioni per gli alberi verificandone la distanza
    private void PosizioniValideAlberi()
    {
        Vector3 possibilenuovaPosizione = Vector3.zero;
        bool cercaNuovaPosizione = true;
        bool troppoVicino = false;

        posizioniAlberi.Add(PosizioneAlbero());
        for (int i = 1; i <= numeroAlberi; i++)
        {
            cercaNuovaPosizione = true;
            //Continuo a cercare una posizione valida finchè non la trovo
            while (cercaNuovaPosizione)
            {
                possibilenuovaPosizione = PosizioneAlbero();
                //Confronto ogni posizione registrata con la possibile nuova posizione
                foreach (Vector3 posizione in posizioniAlberi)
                {
                    if (Vector3.Distance(possibilenuovaPosizione, posizione) < 1.25f)   //Usata distanza arbitraria sapendo la grandezza del prefab
                    {
                        troppoVicino = true;
                        if (debug)
                            Debug.Log("Posizione troppo vicina");
                    }
                }
                //Verifico il risultato del confronto
                if (!troppoVicino)
                {
                    cercaNuovaPosizione = false;
                    //Registro la nuova posizione
                    posizioniAlberi.Add(possibilenuovaPosizione);
                    Debug.Log("Posizione albero valida");
                }
                troppoVicino = false;
            }
        }
    }

    void Start()
    {
        Debug.Log("ATTENZIONE: lo script GenerateForest ha una criticità," +
            " non verifica se per la quantità di alberi inserita l'area di istanziamento è abbastanza grande, potrebbe andare in loop con parametri diversi da questi");
        Forest();
    }

    void Update()
    {
        if (rigera)
        {
            foreach(GameObject albero in alberiForesta)
            {
                Destroy(albero);
            }
            Forest();
            rigera = false;
        }
    }
}
