using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using UnityEngine;

public class CsvTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
	    using (var reader = new StreamReader("path\\to\\file.csv"))
	    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
	    {
		    var records = csv.GetRecords<string>();
	    }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
