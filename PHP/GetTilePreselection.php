<?php

$category = $_POST["category"];

try
{
	$bdd = new PDO('mysql:host=localhost;dbname=tpdemo;charset=utf8', 'root', '', array(PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION, PDO::MYSQL_ATTR_LOCAL_INFILE => true));
}
catch(Exception $e)
{
	die('Error : ' . $e->getMessage());
}

if ($category == "walls") {
	$selectTiles = "SELECT tptiles.libelle FROM tptiles INNER JOIN preselections ON tptiles.id = preselections.tile_id WHERE tptiles.mur=1;";
}
elseif ($category == "slabs") {
		$selectTiles = "SELECT tptiles.libelle FROM tptiles INNER JOIN preselections ON tptiles.id = preselections.tile_id WHERE tptiles.sol=1;";
}
else
{
	$selectTiles = "SELECT tptiles.libelle FROM tptiles INNER JOIN preselections ON tptiles.id = preselections.tile_id;";
}



$result = $bdd->query($selectTiles);

if ($result->errorCode() == 00000) 
{
  echo "Retrieve tiles preselection: OK\r\n";
} 
else 
{
  echo "Error while retrieving tiles preselection!\r\n";
}

if ($result->rowCount() > 0) {
    echo "RETURNS\r\n";
}

// output data of each row
while($row = $result->fetch(PDO::FETCH_ASSOC)) {
    echo ";" . $row['libelle'];
} 

//Close the query access
$result->closeCursor();
?>