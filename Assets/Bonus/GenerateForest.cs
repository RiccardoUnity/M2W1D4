using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class GenerateForest : MonoBehaviour
{
    [Tooltip("Rende visibile i log di debug")]
    public bool debug = true;

    [Tooltip("Materiali possibili per la chioma")]
    public Material[] materials = new Material[5];
    [Tooltip("Possibili asset da istanziare, ATTENZIONE devono avere un oggetto figlio Chioma")]
    public GameObject[] albero = new GameObject[4];
    [Tooltip("Quantità massima di alberi da generare")]
    [Range(5, 40)]
    public int numeroAlberi = 15;
    [Tooltip("Lato della foresta da generare")]
    [Range(5, 10)]
    public float latoForesta = 10f;
    private int latoForestaInt = 0;
    public Transform centroForesta;
    private Vector3 deltaCentroForesta = Vector3.zero;   //Serve per centrare la posizione durante il calcolo random
    [Tooltip("Il nome della chioma dell'albero deve essere lo stesso per ogni GameObject inserito nell'array albero")]
    public string nomeChioma = "Chioma";
    List<GameObject> alberiForesta = new List<GameObject>();
    List<Vector3> posizioniAlberi = new List<Vector3>();
    
    private System.Random random = new System.Random();   //Generare numeri random in un altro Thread
    private int risoluzioneFloat = 1000;   //Per non dover usare double
    private int contoCalcoli = 0;   //Per non dover usare return

    //Potevo usare gli eventi ... ma per provarlo ho preferito fare così
    [Tooltip("Impostare VERO per ricalcolare lo script in RunTime")]
    public bool rigenera = false;

    //ATTENZIONE: BACKGROUND - THREAD SECONDARIO
    //Genero una posizione random per un albero
    private Vector3 PosizioneAlbero()
    {
        return new Vector3((random.Next(0, latoForestaInt)) / risoluzioneFloat,
                            0f,
                            (random.Next(0, latoForestaInt)) / risoluzioneFloat)
            + deltaCentroForesta;
    }

    /*ATTENZIONE: BACKGROUND - THREAD SECONDARIO
    Uso un thread secondario per interrompere il ciclo while così da usare tutta la potenza disponibile
    nell'arco di un secondo, tempo in cui il giocatore, in questo caso, è disposto ad aspettare

    Genero tutte le posizioni per gli alberi verificandone la distanza */
    private void PosizioniValideAlberi(float secondiMassimiCalcoli)
    {
        Vector3 possibilenuovaPosizione = Vector3.zero;
        bool cercaNuovaPosizione = true;
        bool troppoVicino = false;
        //Resetto il conto dei calcoli eseguiti
        contoCalcoli = 0;

        //Avvio il conto del tempo del sistema operativo
        Stopwatch tempo = Stopwatch.StartNew();

        //Inserisco la prima posizione del primo albero sapendo che sarà sicuramente valida e fornisce un mezzo di paragone per i prossimi calcoli
        posizioniAlberi.Add(PosizioneAlbero());
        for (int i = 1; i < numeroAlberi; i++)
        {
            cercaNuovaPosizione = true;
            //Continuo a cercare una posizione valida finchè non la trovo o il tempo finisce
            while (cercaNuovaPosizione && tempo.Elapsed.TotalSeconds < secondiMassimiCalcoli)
            {
                contoCalcoli++;
                possibilenuovaPosizione = PosizioneAlbero();
                //Confronto ogni posizione registrata con la possibile nuova posizione
                foreach (Vector3 posizione in posizioniAlberi)
                {
                    if (Vector3.Distance(possibilenuovaPosizione, posizione) < 1.25f)   //Usata distanza arbitraria sapendo la grandezza del prefab
                    {
                        troppoVicino = true;
                    }
                }
                //Verifico il risultato del confronto
                if (!troppoVicino)
                {
                    cercaNuovaPosizione = false;   //Esco dal ciclo while per calcolare la prossima posizione
                    //Registro la nuova posizione
                    posizioniAlberi.Add(possibilenuovaPosizione);
                }
                troppoVicino = false;
            }
        }
    }

    //Funzione principale per generare una foresta
    //ESEGUITA nel MAIN-THREAD in modo asincrono per aspettare l'esecuzione in background delle posizioni degli alberi
    //Unity continuerà a fare le sue cose mentre si aspetta, il giocatore potrebbe per esempio continuare a muoversi
    async void Forest()
    {
        GameObject nuovoAlbero;
        MeshRenderer chiomaMR;

        int randomTipoAlbero = 0;
        int randomMaterial = 0;

        //Resetto parametro per rigenerare la foresta nell'Update
        rigenera = false;
        latoForestaInt = (int)(risoluzioneFloat * latoForesta);   //Uso variabile int per non dover usare double in background ...
        deltaCentroForesta = centroForesta.position - new Vector3(latoForesta / 2f, 0f, latoForesta / 2f);   //Differenza per centrare l'origine della foresta, usata in background

        //Resetto le liste
        alberiForesta.Clear();
        posizioniAlberi.Clear();

        //Genero la posizione degli alberi in un Thread secondario per non bloccare Unity
        await Task.Run(() => PosizioniValideAlberi(1f));
        if (debug)
        {
            Debug.Log("Calcoli eseguiti: " + contoCalcoli);
            Debug.Log("Alberi generati: " + posizioniAlberi.Count);
        }

        //Genero la foresta
        foreach (Vector3 posAlb in posizioniAlberi)
        {
            //Scelgo una variante dell'albero e lo istanzio
            randomTipoAlbero = Random.Range(0, albero.Length);
            nuovoAlbero = Instantiate(albero[randomTipoAlbero], posAlb, Quaternion.identity);
            alberiForesta.Add(nuovoAlbero);

            //Assegno un nuovo material alla chioma dell'albero appena istanziato
            //I prefab sono tali solo nell'editor e negli assets, non in runtime
            chiomaMR = nuovoAlbero.transform.Find(nomeChioma).GetComponent<MeshRenderer>();
            randomMaterial = Random.Range(0, materials.Length);
            chiomaMR.material = materials[randomMaterial];
        }
    }

    void Start()
    {
        Forest();
    }

    void Update()
    {
        if (rigenera)
        {
            //Cancello la foresta esistente
            foreach(GameObject albero in alberiForesta)
            {
                Destroy(albero);
            }
            //Genero una foresta nuova
            Forest();
        }
    }
}
