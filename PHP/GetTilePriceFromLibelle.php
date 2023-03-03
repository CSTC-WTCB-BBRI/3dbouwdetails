<?php

$libelle = $_POST["libelle"];

try
{
	$bdd = new PDO('mysql:host=localhost;dbname=tpdemo;charset=utf8', 'root', '', array(PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION, PDO::MYSQL_ATTR_LOCAL_INFILE => true));
}
catch(Exception $e)
{
	die('Error : ' . $e->getMessage());
}


$selectCmd = "SELECT prix_vente FROM tptiles WHERE libelle='" . $libelle . "';";

$result = $bdd->query($selectCmd);
if ($result->errorCode() == 00000) 
{
  echo "Price extraction: OK\r\n";
} 
else 
{
  echo "Error extracting the price of tile!\r\n";
}

if ($result->rowCount() > 0) {
    echo "RETURNS\r\n";
}

// output data of each row
while($row = $result->fetch(PDO::FETCH_ASSOC)) {
    echo ";" . $row['prix_vente'];
}

//Close the query access
$result->closeCursor();

?>