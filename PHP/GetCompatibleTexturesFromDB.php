<?php

$category = $_POST["category"];
$tilesTable = "tptiles";

try
{
	$bdd = new PDO('mysql:host=localhost;dbname=tpdemo;charset=utf8', 'root', '', array(PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION, PDO::MYSQL_ATTR_LOCAL_INFILE => true));
}
catch(Exception $e)
{
	die('Error : ' . $e->getMessage());
}

if ($category == "wall") {
	$getCommand = "SELECT `chemin_texture` FROM tptiles INNER JOIN `preselections` ON  preselections.tile_id=tptiles.id WHERE mur=1";
}
else{
	$getCommand = "SELECT `chemin_texture` FROM tptiles INNER JOIN `preselections` ON  preselections.tile_id=tptiles.id WHERE sol=1";
}

$result = $bdd->query($getCommand);

if ($result->errorCode() == 00000) 
{
  echo "Texture paths obtention: OK\r\n";
} 
else 
{
  echo "Error while getting the texture paths!\r\n";
}

if ($result->rowCount() > 0) {
	$rows = array();
    echo "RETURNS\r\n";
}

// output data of each row
while($row = $result->fetch(PDO::FETCH_ASSOC)) {
    //echo ";" . $row['chemin_texture'];
    $rows[] = $row;
} 

//Close the query access
echo json_encode($rows);
$result->closeCursor();
?>